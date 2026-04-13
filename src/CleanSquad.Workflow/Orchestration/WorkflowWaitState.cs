using System;

namespace CleanSquad.Workflow.Orchestration;

/// <summary>
///     Persists one intentional workflow pause created by a wait node.
/// </summary>
public sealed class WorkflowWaitState
{
    /// <summary>
    ///     Gets or sets the wait node identifier that created the pause.
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the next node identifier that should be enqueued once the wait expires.
    /// </summary>
    public string NextNodeId { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the optional parallel group identifier when the wait occurs inside a forked branch.
    /// </summary>
    public string? ParallelGroupId { get; set; }

    /// <summary>
    ///     Gets or sets the optional branch identifier when the wait occurs inside a forked branch.
    /// </summary>
    public string? BranchId { get; set; }

    /// <summary>
    ///     Gets or sets the configured wait duration text captured from the workflow definition.
    /// </summary>
    public string WaitDuration { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the human-readable wait reason.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the UTC timestamp when the wait began.
    /// </summary>
    public DateTimeOffset WaitStartedAtUtc { get; set; }

    /// <summary>
    ///     Gets or sets the earliest UTC timestamp when the workflow may continue.
    /// </summary>
    public DateTimeOffset WaitUntilUtc { get; set; }
}
