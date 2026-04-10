using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CleanSquad.Core.Workflows;

/// <summary>
///     Runs a role-specific workflow stage against an agent backend.
/// </summary>
public interface IWorkflowAgentRunner
{
    /// <summary>
    ///     Executes a workflow stage.
    /// </summary>
    /// <param name="stage">The stage being executed.</param>
    /// <param name="prompt">The prompt to send to the agent.</param>
    /// <param name="attachmentFilePaths">The markdown files that should be attached for the stage.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>The markdown response from the agent.</returns>
    Task<string> RunAsync(
        WorkflowStage stage,
        string prompt,
        IReadOnlyList<string> attachmentFilePaths,
        CancellationToken cancellationToken = default);
}
