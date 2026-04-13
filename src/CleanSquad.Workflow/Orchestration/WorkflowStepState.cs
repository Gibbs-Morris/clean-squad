using System;
using System.Collections.Generic;
using CleanSquad.Workflow.Definitions;

namespace CleanSquad.Workflow.Orchestration;

/// <summary>
///     Captures the persisted state of one workflow step execution.
/// </summary>
public sealed class WorkflowStepState
{
    /// <summary>
    ///     Gets or sets the deterministic step number.
    /// </summary>
    public int StepNumber { get; set; }

    /// <summary>
    ///     Gets or sets the node identifier executed by the step.
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the node kind.
    /// </summary>
    public WorkflowNodeKind NodeKind { get; set; }

    /// <summary>
    ///     Gets or sets the logical role name used by the step.
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the explicit agent/persona identifier used by the step.
    /// </summary>
    public string AgentName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the preferred backend model identifiers captured for the step.
    /// </summary>
    public IReadOnlyList<string> Models { get; set; } = [];

    /// <summary>
    ///     Gets or sets the reasoning-effort preference captured for the step.
    /// </summary>
    public string? ReasoningEffort { get; set; }

    /// <summary>
    ///     Gets or sets the configured response-timeout override captured for the step.
    /// </summary>
    public string? ResponseTimeout { get; set; }

    /// <summary>
    ///     Gets or sets the input references captured for the step.
    /// </summary>
    public IReadOnlyList<string> InputReferences { get; set; } = [];

    /// <summary>
    ///     Gets or sets the declared output names captured for the step.
    /// </summary>
    public IReadOnlyList<string> OutputNames { get; set; } = [];

    /// <summary>
    ///     Gets or sets the optional custom message captured for the step.
    /// </summary>
    public string? CustomMessage { get; set; }

    /// <summary>
    ///     Gets or sets the attempt count for the node.
    /// </summary>
    public int Attempt { get; set; }

    /// <summary>
    ///     Gets or sets the activation sequence that created the step.
    /// </summary>
    public int ActivationSequenceNumber { get; set; }

    /// <summary>
    ///     Gets or sets the output artifact path for the step.
    /// </summary>
    public string? OutputPath { get; set; }

    /// <summary>
    ///     Gets or sets the selected decision choice identifier when the step is a decision.
    /// </summary>
    public string? ChoiceId { get; set; }

    /// <summary>
    ///     Gets or sets the next node identifier selected by the step.
    /// </summary>
    public string? NextNodeId { get; set; }

    /// <summary>
    ///     Gets or sets the step status.
    /// </summary>
    public WorkflowStepStatus Status { get; set; } = WorkflowStepStatus.InProgress;

    /// <summary>
    ///     Gets or sets the optional parallel group identifier.
    /// </summary>
    public string? ParallelGroupId { get; set; }

    /// <summary>
    ///     Gets or sets the optional branch identifier.
    /// </summary>
    public string? BranchId { get; set; }

    /// <summary>
    ///     Gets or sets the start timestamp in UTC.
    /// </summary>
    public DateTimeOffset StartedAtUtc { get; set; }

    /// <summary>
    ///     Gets or sets the completion timestamp in UTC.
    /// </summary>
    public DateTimeOffset? CompletedAtUtc { get; set; }

    /// <summary>
    ///     Gets or sets an optional detail message.
    /// </summary>
    public string? Message { get; set; }
}
