using System.Threading;
using System.Threading.Tasks;

namespace CleanSquad.Core.Workflows;

/// <summary>
///     Coordinates the planner, builder, reviewer, and optional rebuild workflow.
/// </summary>
public interface IWorkflowOrchestrator
{
    /// <summary>
    ///     Executes the workflow for a request markdown document.
    /// </summary>
    /// <param name="workspaceRootPath">The workspace root path used to store workflow artifacts.</param>
    /// <param name="requestDocumentPath">The source request markdown path.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>The workflow result.</returns>
    Task<WorkflowRunResult> ExecuteAsync(
        string workspaceRootPath,
        string requestDocumentPath,
        CancellationToken cancellationToken = default);
}
