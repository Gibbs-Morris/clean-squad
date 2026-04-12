namespace CleanSquad.Workflow;

/// <summary>
///     Describes the final state of a workflow run.
/// </summary>
public enum WorkflowRunStatus
{
    /// <summary>
    ///     The workflow is in progress and may be resumed.
    /// </summary>
    Running,

    /// <summary>
    ///     The workflow completed with approval.
    /// </summary>
    Approved,

    /// <summary>
    ///     The workflow stopped without approval.
    /// </summary>
    Stopped,

    /// <summary>
    ///     The workflow failed before reaching an exit node.
    /// </summary>
    Failed,
}
