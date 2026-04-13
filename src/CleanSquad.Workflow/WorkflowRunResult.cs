using System.Collections.Generic;
using CleanSquad.Workflow.Decisions;

namespace CleanSquad.Workflow;

/// <summary>
///     Captures the outcome of a workflow execution.
/// </summary>
/// <param name="RunDirectoryPath">The folder containing the generated workflow artifacts.</param>
/// <param name="FinalArtifactPath">The path to the final workflow markdown artifact.</param>
/// <param name="StateArtifactPath">The path to the workflow state markdown artifact.</param>
/// <param name="Status">The final workflow status.</param>
/// <param name="ReviewApproved">Whether the workflow ended in approval.</param>
/// <param name="RebuildCount">The number of rebuild passes executed.</param>
/// <param name="ReviewCycleCount">The number of review cycles executed.</param>
/// <param name="Decisions">The decisions taken during workflow execution.</param>
public sealed record WorkflowRunResult(
    string RunDirectoryPath,
    string FinalArtifactPath,
    string StateArtifactPath,
    WorkflowRunStatus Status,
    bool ReviewApproved,
    int RebuildCount,
    int ReviewCycleCount,
    IReadOnlyList<WorkflowDecision> Decisions)
{
    /// <summary>
    ///     Gets a concise user-facing summary of the outcome.
    /// </summary>
    public string StatusLabel => this.Status switch
    {
        WorkflowRunStatus.Running => "is still running",
        WorkflowRunStatus.Paused => "paused and is waiting to be resumed",
        WorkflowRunStatus.Approved when this.RebuildCount > 0 => $"finished with approval after {this.RebuildCount} rebuild{(this.RebuildCount == 1 ? string.Empty : "s")}",
        WorkflowRunStatus.Approved => "finished with reviewer approval",
        WorkflowRunStatus.Failed => "failed before reaching an exit node",
        _ => "stopped without approval",
    };
}
