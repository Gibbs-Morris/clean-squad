namespace CleanSquad.Workflow.Orchestration;

/// <summary>
///     Represents one pending node activation waiting to execute.
/// </summary>
public sealed class WorkflowPendingActivation
{
    /// <summary>
    ///     Gets or sets the activation sequence number.
    /// </summary>
    public int SequenceNumber { get; set; }

    /// <summary>
    ///     Gets or sets the target node identifier.
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the parallel group identifier when the activation belongs to a forked branch.
    /// </summary>
    public string? ParallelGroupId { get; set; }

    /// <summary>
    ///     Gets or sets the branch identifier when the activation belongs to a forked branch.
    /// </summary>
    public string? BranchId { get; set; }
}
