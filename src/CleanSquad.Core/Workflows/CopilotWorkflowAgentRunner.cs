using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CleanSquad.Workflow;
using GitHub.Copilot.SDK;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CleanSquad.Core.Workflows;

/// <summary>
///     Executes workflow stages by calling the GitHub Copilot SDK.
/// </summary>
public sealed partial class CopilotWorkflowAgentRunner : IWorkflowAgentRunner
{
    private readonly string workspaceRootPath;
    private readonly TimeSpan responseTimeout;
    private readonly ILogger<CopilotWorkflowAgentRunner> logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CopilotWorkflowAgentRunner" /> class.
    /// </summary>
    /// <param name="workspaceRootPath">The workspace root path used as the Copilot CLI working directory.</param>
    public CopilotWorkflowAgentRunner(string workspaceRootPath)
        : this(
            Options.Create(new CopilotWorkflowAgentRunnerOptions
            {
                WorkspaceRootPath = workspaceRootPath,
            }),
            null)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="CopilotWorkflowAgentRunner" /> class.
    /// </summary>
    /// <param name="options">The Copilot workflow agent runner options.</param>
    /// <param name="logger">The optional logger.</param>
    public CopilotWorkflowAgentRunner(
        IOptions<CopilotWorkflowAgentRunnerOptions>? options,
        ILogger<CopilotWorkflowAgentRunner>? logger = null)
    {
        CopilotWorkflowAgentRunnerOptions effectiveOptions = (options ?? Options.Create(new CopilotWorkflowAgentRunnerOptions())).Value;
        ArgumentException.ThrowIfNullOrWhiteSpace(effectiveOptions.WorkspaceRootPath);
        this.workspaceRootPath = Path.GetFullPath(effectiveOptions.WorkspaceRootPath);
        this.responseTimeout = effectiveOptions.ResponseTimeout ?? TimeSpan.FromMinutes(5);
        if (this.responseTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), effectiveOptions.ResponseTimeout, "The Copilot workflow response timeout must be greater than zero.");
        }

        this.logger = logger ?? NullLogger<CopilotWorkflowAgentRunner>.Instance;
    }

    /// <inheritdoc />
    public async Task<string> RunAsync(
        string agentName,
        string prompt,
        IReadOnlyList<string> attachmentFilePaths,
        IReadOnlyList<string> modelIds,
        string? reasoningEffort,
        TimeSpan? responseTimeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        ArgumentNullException.ThrowIfNull(attachmentFilePaths);
        ArgumentNullException.ThrowIfNull(modelIds);
        cancellationToken.ThrowIfCancellationRequested();
        string? modelId = SelectPreferredModelId(modelIds);
        string? configuredReasoningEffort = WorkflowReasoningEffort.Normalize(reasoningEffort);
        TimeSpan effectiveResponseTimeout = responseTimeout ?? this.responseTimeout;
        string modelSummary = modelIds.Count == 0 ? "(default)" : string.Join(",", modelIds);
        string attachmentSummary = FormatAttachmentSummary(attachmentFilePaths);
        string reasoningSummary = configuredReasoningEffort ?? "(model default)";
        string promptWithContext = await BuildPromptWithContextAsync(prompt, attachmentFilePaths, cancellationToken);
        this.LogExecutingRole(
            agentName,
            this.workspaceRootPath,
            attachmentFilePaths.Count,
            modelSummary,
            reasoningSummary,
            effectiveResponseTimeout.ToString("c", CultureInfo.InvariantCulture),
            promptWithContext.Length,
            attachmentSummary);

        try
        {
            await using var client = new CopilotClient(new CopilotClientOptions
            {
                Cwd = this.workspaceRootPath,
            });

            await client.StartAsync(cancellationToken);
            string? resolvedReasoningEffort = await ResolveReasoningEffortAsync(client, modelId, configuredReasoningEffort, cancellationToken);
            await using var session = await client.CreateSessionAsync(CreateSessionConfig(this.workspaceRootPath, modelId, resolvedReasoningEffort), cancellationToken);

            AssistantMessageEvent? reply = await session.SendAndWaitAsync(
                new MessageOptions
                {
                    Prompt = promptWithContext,
                },
                effectiveResponseTimeout,
                cancellationToken);

            string? content = reply?.Data.Content;
            if (string.IsNullOrWhiteSpace(content))
            {
                InvalidOperationException exception = new($"The {agentName} agent returned an empty response.");
                this.LogEmptyResponse(exception, agentName);
                throw exception;
            }

            string markdown = content.Trim();
            this.LogRoleCompleted(
                agentName,
                markdown.Length);
            return markdown;
        }
        catch (TimeoutException exception)
        {
            string diagnosticMessage = BuildTimeoutDiagnosticMessage(
                agentName,
                effectiveResponseTimeout,
                promptWithContext.Length,
                attachmentSummary,
                modelSummary,
                reasoningSummary);
            TimeoutException diagnosticException = new(diagnosticMessage, exception);
            this.LogRoleTimedOut(
                diagnosticException,
                agentName,
                effectiveResponseTimeout.ToString("c", CultureInfo.InvariantCulture),
                promptWithContext.Length,
                attachmentSummary,
                modelSummary,
                reasoningSummary);
            throw diagnosticException;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            this.LogRoleFailed(exception, agentName, promptWithContext.Length, attachmentSummary, modelSummary, reasoningSummary);
            throw;
        }
    }

    private static async Task<string> BuildPromptWithContextAsync(
        string prompt,
        IReadOnlyList<string> attachmentFilePaths,
        CancellationToken cancellationToken)
    {
        StringBuilder builder = new();
        builder.AppendLine(prompt.Trim());
        builder.AppendLine();
        builder.AppendLine("## Allowed Markdown Context");
        builder.AppendLine();

        foreach (string filePath in attachmentFilePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string fileName = Path.GetFileName(filePath);
            string markdownContent = await File.ReadAllTextAsync(filePath, cancellationToken);

            builder.Append("### ");
            builder.AppendLine(fileName);
            builder.AppendLine();
            builder.AppendLine(markdownContent.Trim());
            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }

    /// <summary>
    ///     Creates the SDK session configuration used for workflow-backed Copilot interactions.
    /// </summary>
    /// <param name="workspaceRootPath">The workspace root that the session should treat as its working directory.</param>
    /// <param name="modelId">The preferred model identifier for the current workflow stage.</param>
    /// <param name="reasoningEffort">The resolved reasoning-effort preference for the selected model.</param>
    /// <returns>The configured session settings.</returns>
    internal static SessionConfig CreateSessionConfig(string workspaceRootPath, string? modelId, string? reasoningEffort)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRootPath);

        return new SessionConfig
        {
            WorkingDirectory = workspaceRootPath,
            Model = modelId,
            ReasoningEffort = WorkflowReasoningEffort.Normalize(reasoningEffort),
            OnPermissionRequest = PermissionHandler.ApproveAll,
        };
    }

    /// <summary>
    ///     Resolves the configured reasoning-effort request to an SDK-supported value.
    /// </summary>
    /// <param name="client">The started Copilot client.</param>
    /// <param name="modelId">The selected model identifier.</param>
    /// <param name="configuredReasoningEffort">The configured reasoning-effort request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The resolved reasoning-effort value to send to the SDK, or <see langword="null" />.</returns>
    internal static async Task<string?> ResolveReasoningEffortAsync(
        CopilotClient client,
        string? modelId,
        string? configuredReasoningEffort,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);

        string? normalizedReasoningEffort = WorkflowReasoningEffort.Normalize(configuredReasoningEffort);
        if (normalizedReasoningEffort is null)
        {
            return null;
        }

        if (!WorkflowReasoningEffort.IsHighestSupportedValue(normalizedReasoningEffort)
            || string.IsNullOrWhiteSpace(modelId))
        {
            return normalizedReasoningEffort;
        }

        IReadOnlyList<ModelInfo> availableModels = await client.ListModelsAsync(cancellationToken);
        ModelInfo? selectedModel = availableModels.FirstOrDefault(availableModel =>
            string.Equals(availableModel.Id, modelId, StringComparison.OrdinalIgnoreCase));
        if (selectedModel is null)
        {
            return null;
        }

        return ResolveHighestSupportedReasoningEffort(selectedModel.SupportedReasoningEfforts)
            ?? selectedModel.DefaultReasoningEffort;
    }

    /// <summary>
    ///     Resolves the highest supported reasoning-effort value reported for a model.
    /// </summary>
    /// <param name="supportedReasoningEfforts">The supported reasoning-effort values reported by the model metadata.</param>
    /// <returns>The highest supported reasoning-effort value, or <see langword="null" />.</returns>
    internal static string? ResolveHighestSupportedReasoningEffort(IReadOnlyList<string>? supportedReasoningEfforts)
    {
        return WorkflowReasoningEffort.SelectHighestSupported(supportedReasoningEfforts);
    }

    private static string? SelectPreferredModelId(IReadOnlyList<string> modelIds)
    {
        ArgumentNullException.ThrowIfNull(modelIds);

        return modelIds.FirstOrDefault(modelId => !string.IsNullOrWhiteSpace(modelId))?.Trim();
    }

    private static string FormatAttachmentSummary(IReadOnlyList<string> attachmentFilePaths)
    {
        if (attachmentFilePaths.Count == 0)
        {
            return "(none)";
        }

        return string.Join(
            ", ",
            attachmentFilePaths.Select(path =>
            {
                FileInfo fileInfo = new(path);
                long length = fileInfo.Exists ? fileInfo.Length : 0;
                return $"{Path.GetFileName(path)} ({length.ToString(CultureInfo.InvariantCulture)} bytes)";
            }));
    }

    private static string BuildTimeoutDiagnosticMessage(
        string agentName,
        TimeSpan responseTimeout,
        int promptLength,
        string attachmentSummary,
        string modelSummary,
        string reasoningEffort)
    {
        return $"Agent '{agentName}' timed out after waiting {responseTimeout.ToString("c", CultureInfo.InvariantCulture)} for a Copilot response. Prompt length: {promptLength.ToString(CultureInfo.InvariantCulture)} characters. Attachments: {attachmentSummary}. Models: {modelSummary}. Reasoning effort: {reasoningEffort}. This usually means the stage needed more time to edit, build, or test, or the requested increment was too large for one pass.";
    }

    [LoggerMessage(EventId = 100, Level = LogLevel.Debug, Message = "Executing Copilot workflow role {RoleName} from workspace {WorkspaceRootPath} with {AttachmentCount} attachments, models {Models}, reasoning effort {ReasoningEffort}, timeout {ResponseTimeout}, prompt length {PromptLength}, and attachments {AttachmentSummary}.")]
    private partial void LogExecutingRole(string roleName, string workspaceRootPath, int attachmentCount, string models, string reasoningEffort, string responseTimeout, int promptLength, string attachmentSummary);

    [LoggerMessage(EventId = 101, Level = LogLevel.Error, Message = "Copilot workflow role {RoleName} returned an empty response.")]
    private partial void LogEmptyResponse(Exception exception, string roleName);

    [LoggerMessage(EventId = 102, Level = LogLevel.Debug, Message = "Copilot workflow role {RoleName} completed successfully with {CharacterCount} characters.")]
    private partial void LogRoleCompleted(string roleName, int characterCount);

    [LoggerMessage(EventId = 103, Level = LogLevel.Error, Message = "Copilot workflow role {RoleName} failed after preparing {PromptLength} characters with attachments {AttachmentSummary}, models {Models}, and reasoning effort {ReasoningEffort}.")]
    private partial void LogRoleFailed(Exception exception, string roleName, int promptLength, string attachmentSummary, string models, string reasoningEffort);

    [LoggerMessage(EventId = 104, Level = LogLevel.Error, Message = "Copilot workflow role {RoleName} timed out after {ResponseTimeout} with prompt length {PromptLength}, attachments {AttachmentSummary}, models {Models}, and reasoning effort {ReasoningEffort}.")]
    private partial void LogRoleTimedOut(Exception exception, string roleName, string responseTimeout, int promptLength, string attachmentSummary, string models, string reasoningEffort);
}
