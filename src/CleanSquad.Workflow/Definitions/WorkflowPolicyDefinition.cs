namespace CleanSquad.Workflow.Definitions;

/// <summary>
///     Defines the bounded execution policies for a workflow.
/// </summary>
public sealed class WorkflowPolicyDefinition
{
    /// <summary>
    ///     Gets or sets how decisions are resolved after a review.
    /// </summary>
    public WorkflowDecisionMode DecisionMode { get; set; } = WorkflowDecisionMode.Rules;

    /// <summary>
    ///     Gets or sets the maximum number of review cycles that may execute.
    /// </summary>
    public int MaxReviewCycles { get; set; } = 2;

    /// <summary>
    ///     Gets or sets the maximum number of rebuild passes that may execute.
    /// </summary>
    public int MaxRebuilds { get; set; } = 1;

    /// <summary>
    ///     Gets or sets the maximum number of stage nodes that may run at the same time.
    /// </summary>
    public int MaxParallelism { get; set; } = 4;

    /// <summary>
    ///     Gets or sets the maximum number of times the same node may execute in one run.
    /// </summary>
    public int MaxNodeVisits { get; set; } = 10;
}
