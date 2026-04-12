namespace CleanSquad.Workflow;

/// <summary>
///     Describes how a workflow should be started or resumed.
/// </summary>
/// <param name="WorkspaceRootPath">The workspace root path.</param>
/// <param name="WorkflowDefinitionPath">The workflow definition path for a new run.</param>
/// <param name="RequestDocumentPath">The request markdown path for a new run.</param>
/// <param name="EntryPointId">The optional entry point or node identifier to start from.</param>
/// <param name="ResumePath">The optional run directory or state file path to resume.</param>
public sealed record WorkflowExecutionRequest(
    string WorkspaceRootPath,
    string? WorkflowDefinitionPath,
    string? RequestDocumentPath,
    string? EntryPointId = null,
    string? ResumePath = null);
