namespace CleanSquad.Workflow.Orchestration;

/// <summary>
///     Describes the lifecycle state of one workflow step.
/// </summary>
public enum WorkflowStepStatus
{
    /// <summary>
    ///     The step has started but not yet completed.
    /// </summary>
    InProgress,

    /// <summary>
    ///     The step completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    ///     The step failed.
    /// </summary>
    Failed,
}
