using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using CleanSquad.Workflow.Orchestration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CleanSquad.Workflow.Storage;

/// <summary>
///     Creates, writes, and normalizes the artifacts used by the workflow.
/// </summary>
public sealed partial class MarkdownArtifactService : IWorkflowArtifactService
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(false);
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly TimeProvider timeProvider;
    private readonly ILogger<MarkdownArtifactService> logger;
    private readonly WorkflowStorageOptions storageOptions;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MarkdownArtifactService" /> class.
    /// </summary>
    /// <param name="timeProvider">The time provider used to create deterministic run identifiers.</param>
    public MarkdownArtifactService(TimeProvider? timeProvider = null)
        : this(null, null, timeProvider)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="MarkdownArtifactService" /> class.
    /// </summary>
    /// <param name="storageOptions">The workflow storage options.</param>
    /// <param name="logger">The optional logger.</param>
    /// <param name="timeProvider">The time provider used to create deterministic run identifiers.</param>
    public MarkdownArtifactService(
        IOptions<WorkflowStorageOptions>? storageOptions,
        ILogger<MarkdownArtifactService>? logger = null,
        TimeProvider? timeProvider = null)
    {
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.logger = logger ?? NullLogger<MarkdownArtifactService>.Instance;
        this.storageOptions = (storageOptions ?? Options.Create(new WorkflowStorageOptions())).Value;
    }

    /// <inheritdoc />
    public WorkflowArtifacts CreateRunArtifacts(string workspaceRootPath, string workflowDefinitionPath, string sourceRequestPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowDefinitionPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRequestPath);

        if (!File.Exists(workflowDefinitionPath))
        {
            throw new FileNotFoundException("The workflow definition file could not be found.", workflowDefinitionPath);
        }

        if (!File.Exists(sourceRequestPath))
        {
            throw new FileNotFoundException("The request markdown file could not be found.", sourceRequestPath);
        }

        WorkflowArtifacts artifacts = WorkflowArtifacts.Create(workspaceRootPath, workflowDefinitionPath, sourceRequestPath, this.timeProvider, this.storageOptions);
        Directory.CreateDirectory(artifacts.RunDirectoryPath);
        Directory.CreateDirectory(artifacts.StepsDirectoryPath);

        File.Copy(Path.GetFullPath(workflowDefinitionPath), artifacts.WorkflowDefinitionPath, true);
        this.WriteMarkdown(artifacts.RequestMarkdownPath, ReadMarkdown(sourceRequestPath));
        this.LogCreatedArtifacts(
            artifacts.RunDirectoryPath,
            artifacts.RequestMarkdownPath);

        return artifacts;
    }

    /// <inheritdoc />
    public WorkflowArtifacts LoadRunArtifacts(string runPathOrStatePath)
    {
        WorkflowArtifacts artifacts = WorkflowArtifacts.LoadExisting(runPathOrStatePath);
        this.LogLoadedArtifacts(artifacts.RunDirectoryPath);
        return artifacts;
    }

    /// <inheritdoc />
    public void WriteMarkdown(string path, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(content);

        string? directoryPath = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        File.WriteAllText(path, NormalizeMarkdown(content), Utf8WithoutBom);
        this.LogWroteMarkdown(path);
    }

    /// <inheritdoc />
    public WorkflowRunState ReadState(WorkflowArtifacts artifacts)
    {
        ArgumentNullException.ThrowIfNull(artifacts);

        if (!File.Exists(artifacts.StateJsonPath))
        {
            throw new FileNotFoundException("The workflow state file could not be found.", artifacts.StateJsonPath);
        }

        string json = File.ReadAllText(artifacts.StateJsonPath, Encoding.UTF8);
        this.LogReadState(artifacts.StateJsonPath);
        return JsonSerializer.Deserialize<WorkflowRunState>(json, JsonSerializerOptions)
            ?? throw new InvalidOperationException("The workflow state JSON could not be deserialized.");
    }

    /// <inheritdoc />
    public void WriteState(WorkflowArtifacts artifacts, WorkflowRunState state)
    {
        ArgumentNullException.ThrowIfNull(artifacts);
        ArgumentNullException.ThrowIfNull(state);

        WriteAllText(artifacts.StateJsonPath, JsonSerializer.Serialize(state, JsonSerializerOptions));

        IEnumerable<string> decisionLines = state.Decisions.Select((decision, index) =>
            $"- {index + 1:00}: {decision.Action} ({decision.Source}) - {decision.Reason}");
        IEnumerable<string> pendingLines = state.PendingActivations.Select(activation =>
            $"- {activation.SequenceNumber:0000}: {activation.NodeId}{FormatBranchSuffix(activation.BranchId)}");
        IEnumerable<string> stepLines = state.Steps.OrderBy(step => step.StepNumber).Select(step =>
            $"- {step.StepNumber:0000}: {step.NodeId} [{step.Status}] attempt={step.Attempt}");
        string stateMarkdown = $"""
# Workflow State
Status: {state.Status}
RunId: {artifacts.RunId}
Workflow: {state.WorkflowName}
EntryNode: {state.EntryNodeId}
ExitNode: {state.ExitNodeId ?? "(not reached)"}
StartedUtc: {state.StartedAtUtc:O}
UpdatedUtc: {state.UpdatedAtUtc:O}
CompletedUtc: {(state.CompletedAtUtc.HasValue ? state.CompletedAtUtc.Value.ToString("O", System.Globalization.CultureInfo.InvariantCulture) : "(not completed)")}

## Files
- workflow: {Path.GetFileName(artifacts.WorkflowDefinitionPath)}
- request: {Path.GetFileName(artifacts.RequestMarkdownPath)}
- final: {Path.GetFileName(artifacts.FinalMarkdownPath)}
- state-json: {Path.GetFileName(artifacts.StateJsonPath)}
- events: {Path.GetFileName(artifacts.EventLogPath)}

## Pending Activations
{string.Join(Environment.NewLine, pendingLines.DefaultIfEmpty("- none"))}

## Steps
{string.Join(Environment.NewLine, stepLines.DefaultIfEmpty("- none"))}

## Decisions
{string.Join(Environment.NewLine, decisionLines.DefaultIfEmpty("- none"))}
""";

        this.WriteMarkdown(artifacts.StateMarkdownPath, stateMarkdown);
        this.LogPersistedState(artifacts.RunId);
    }

    /// <inheritdoc />
    public void AppendLog(WorkflowArtifacts artifacts, WorkflowLogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(artifacts);
        ArgumentNullException.ThrowIfNull(entry);

        string? directoryPath = Path.GetDirectoryName(artifacts.EventLogPath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string line = JsonSerializer.Serialize(entry, JsonSerializerOptions);
        File.AppendAllText(artifacts.EventLogPath, line + Environment.NewLine, Utf8WithoutBom);
        this.LogAppendedEvent(entry.EventName, artifacts.RunId);
    }

    /// <summary>
    ///     Reads a markdown file and normalizes its line endings.
    /// </summary>
    /// <param name="path">The markdown file path.</param>
    /// <returns>The normalized markdown content.</returns>
    public static string ReadMarkdown(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string content = File.ReadAllText(path, Encoding.UTF8);
        return NormalizeMarkdown(content);
    }

    /// <summary>
    ///     Normalizes markdown to LF line endings and ensures exactly one trailing newline.
    /// </summary>
    /// <param name="content">The markdown content to normalize.</param>
    /// <returns>The normalized markdown content.</returns>
    private static string NormalizeMarkdown(string content)
    {
        string normalized = content.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');

        return normalized.TrimEnd() + "\n";
    }

    /// <summary>
    ///     Writes UTF-8 text without BOM and ensures the destination directory exists.
    /// </summary>
    /// <param name="path">The destination file path.</param>
    /// <param name="content">The text content to write.</param>
    private static void WriteAllText(string path, string content)
    {
        string? directoryPath = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        File.WriteAllText(path, content, Utf8WithoutBom);
    }

    /// <summary>
    ///     Formats a human-readable branch suffix for state markdown output.
    /// </summary>
    /// <param name="branchId">The optional branch identifier.</param>
    /// <returns>The formatted branch suffix, or an empty string when no branch is provided.</returns>
    private static string FormatBranchSuffix(string? branchId)
    {
        return string.IsNullOrWhiteSpace(branchId) ? string.Empty : $" (branch={branchId})";
    }

    [LoggerMessage(EventId = 200, Level = LogLevel.Information, Message = "Created workflow artifacts at {RunDirectoryPath} for request {RequestPath}.")]
    private partial void LogCreatedArtifacts(string runDirectoryPath, string requestPath);

    [LoggerMessage(EventId = 201, Level = LogLevel.Information, Message = "Loaded existing workflow artifacts from {RunDirectoryPath}.")]
    private partial void LogLoadedArtifacts(string runDirectoryPath);

    [LoggerMessage(EventId = 202, Level = LogLevel.Debug, Message = "Wrote markdown artifact to {ArtifactPath}.")]
    private partial void LogWroteMarkdown(string artifactPath);

    [LoggerMessage(EventId = 203, Level = LogLevel.Debug, Message = "Read workflow state from {StateJsonPath}.")]
    private partial void LogReadState(string stateJsonPath);

    [LoggerMessage(EventId = 204, Level = LogLevel.Debug, Message = "Persisted workflow state for run {RunId}.")]
    private partial void LogPersistedState(string runId);

    [LoggerMessage(EventId = 205, Level = LogLevel.Debug, Message = "Appended workflow log event {EventName} for run {RunId}.")]
    private partial void LogAppendedEvent(string eventName, string runId);
}
