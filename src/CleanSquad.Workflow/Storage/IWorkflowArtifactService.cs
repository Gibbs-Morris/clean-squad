using CleanSquad.Workflow.Orchestration;

namespace CleanSquad.Workflow.Storage;

/// <summary>
///     Creates and writes the artifacts used by a workflow run.
/// </summary>
public interface IWorkflowArtifactService
{
    /// <summary>
    ///     Creates a new run folder and copies the workflow definition and request files into it.
    /// </summary>
    /// <param name="workspaceRootPath">The workspace root path.</param>
    /// <param name="workflowDefinitionPath">The source workflow definition file.</param>
    /// <param name="sourceRequestPath">The source request markdown path.</param>
    /// <returns>The created workflow artifact set.</returns>
    WorkflowArtifacts CreateRunArtifacts(string workspaceRootPath, string workflowDefinitionPath, string sourceRequestPath);

    /// <summary>
    ///     Loads the artifact paths for an existing run.
    /// </summary>
    /// <param name="runPathOrStatePath">The run directory path or persisted state path.</param>
    /// <returns>The loaded workflow artifact set.</returns>
    WorkflowArtifacts LoadRunArtifacts(string runPathOrStatePath);

    /// <summary>
    ///     Writes normalized markdown content to disk.
    /// </summary>
    /// <param name="path">The target path.</param>
    /// <param name="content">The markdown content to write.</param>
    void WriteMarkdown(string path, string content);

    /// <summary>
    ///     Reads the persisted workflow state.
    /// </summary>
    /// <param name="artifacts">The workflow artifact set.</param>
    /// <returns>The persisted state.</returns>
    WorkflowRunState ReadState(WorkflowArtifacts artifacts);

    /// <summary>
    ///     Writes the persisted workflow state and markdown summary.
    /// </summary>
    /// <param name="artifacts">The workflow artifact set.</param>
    /// <param name="state">The workflow state.</param>
    void WriteState(WorkflowArtifacts artifacts, WorkflowRunState state);

    /// <summary>
    ///     Appends one structured workflow log entry.
    /// </summary>
    /// <param name="artifacts">The workflow artifact set.</param>
    /// <param name="entry">The log entry.</param>
    void AppendLog(WorkflowArtifacts artifacts, WorkflowLogEntry entry);
}
