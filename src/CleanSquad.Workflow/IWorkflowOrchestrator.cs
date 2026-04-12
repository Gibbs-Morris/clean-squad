using System.Threading;
using System.Threading.Tasks;

namespace CleanSquad.Workflow;

/// <summary>
///     Coordinates execution of a configured workflow definition.
/// </summary>
public interface IWorkflowOrchestrator
{
    /// <summary>
    ///     Executes or resumes a workflow from an execution request.
    /// </summary>
    /// <param name="request">The workflow execution request.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>The workflow result.</returns>
    Task<WorkflowRunResult> ExecuteAsync(
        WorkflowExecutionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Executes the workflow for a definition file and request markdown document.
    /// </summary>
    /// <param name="workspaceRootPath">The workspace root path used to store workflow artifacts.</param>
    /// <param name="workflowDefinitionPath">The workflow definition JSON path.</param>
    /// <param name="requestDocumentPath">The source request markdown path.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>The workflow result.</returns>
    Task<WorkflowRunResult> ExecuteAsync(
        string workspaceRootPath,
        string workflowDefinitionPath,
        string requestDocumentPath,
        CancellationToken cancellationToken = default);
}
