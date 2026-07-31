namespace CleanSquad.Workflow.Decisions;

/// <summary>
///     Describes the action selected after a review cycle.
/// </summary>
public enum WorkflowDecisionAction
{
    /// <summary>
    ///     Accept the current build and complete the workflow.
    /// </summary>
    Approve,

    /// <summary>
    ///     Request another build iteration.
    /// </summary>
    Rebuild,

    /// <summary>
    ///     Stop the workflow without approval.
    /// </summary>
    Stop,

    /// <summary>
    ///     Follow a named non-legacy branch.
    /// </summary>
    Branch,
}
