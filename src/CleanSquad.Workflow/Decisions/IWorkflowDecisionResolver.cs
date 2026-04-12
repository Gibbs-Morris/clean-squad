using System.Threading;
using System.Threading.Tasks;

namespace CleanSquad.Workflow.Decisions;

/// <summary>
///     Resolves the next action to take after a review cycle.
/// </summary>
public interface IWorkflowDecisionResolver
{
    /// <summary>
    ///     Resolves the next action for the current workflow state.
    /// </summary>
    /// <param name="context">The decision context.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>The resolved workflow decision.</returns>
    Task<WorkflowDecision> ResolveAsync(WorkflowDecisionContext context, CancellationToken cancellationToken = default);
}
