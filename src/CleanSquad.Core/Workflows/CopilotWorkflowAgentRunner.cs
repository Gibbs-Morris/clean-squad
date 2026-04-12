using System;
using System.Collections.Generic;
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
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        ArgumentNullException.ThrowIfNull(attachmentFilePaths);
        ArgumentNullException.ThrowIfNull(modelIds);
        cancellationToken.ThrowIfCancellationRequested();
        this.LogExecutingRole(
            agentName,
            this.workspaceRootPath,
            attachmentFilePaths.Count,
            modelIds.Count == 0 ? "(default)" : string.Join(",", modelIds));
        string promptWithContext = await BuildPromptWithContextAsync(prompt, attachmentFilePaths, cancellationToken);

        try
        {
            await using var client = new CopilotClient(new CopilotClientOptions
            {
                Cwd = this.workspaceRootPath,
            });

            await using var session = await client.CreateSessionAsync(CreateSessionConfig(this.workspaceRootPath, modelIds), cancellationToken);

            AssistantMessageEvent? reply = await session.SendAndWaitAsync(
                new MessageOptions
                {
                    Prompt = promptWithContext,
                },
                this.responseTimeout,
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
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            this.LogRoleFailed(exception, agentName);
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
    /// <param name="modelIds">The preferred model identifiers for the current workflow stage.</param>
    /// <returns>The configured session settings.</returns>
    internal static SessionConfig CreateSessionConfig(string workspaceRootPath, IReadOnlyList<string> modelIds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRootPath);
        ArgumentNullException.ThrowIfNull(modelIds);

        return new SessionConfig
        {
            WorkingDirectory = workspaceRootPath,
            Model = modelIds.Count > 0 ? modelIds[0] : null,
            OnPermissionRequest = PermissionHandler.ApproveAll,
        };
    }

    [LoggerMessage(EventId = 100, Level = LogLevel.Debug, Message = "Executing Copilot workflow role {RoleName} from workspace {WorkspaceRootPath} with {AttachmentCount} attachments and models {Models}.")]
    private partial void LogExecutingRole(string roleName, string workspaceRootPath, int attachmentCount, string models);

    [LoggerMessage(EventId = 101, Level = LogLevel.Error, Message = "Copilot workflow role {RoleName} returned an empty response.")]
    private partial void LogEmptyResponse(Exception exception, string roleName);

    [LoggerMessage(EventId = 102, Level = LogLevel.Debug, Message = "Copilot workflow role {RoleName} completed successfully with {CharacterCount} characters.")]
    private partial void LogRoleCompleted(string roleName, int characterCount);

    [LoggerMessage(EventId = 103, Level = LogLevel.Error, Message = "Copilot workflow role {RoleName} failed.")]
    private partial void LogRoleFailed(Exception exception, string roleName);
}
