using System.Collections.Generic;
using CleanSquad.Workflow;

namespace CleanSquad.Workflow.Definitions;

/// <summary>
///     Describes one node in the workflow execution graph.
/// </summary>
public sealed class WorkflowNodeDefinition
{
    /// <summary>
    ///     Gets or sets the unique node identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the node kind.
    /// </summary>
    public WorkflowNodeKind Kind { get; set; } = WorkflowNodeKind.Stage;

    /// <summary>
    ///     Gets or sets the display name shown in prompts and logs.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    ///     Gets or sets the logical role name used for agent execution.
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    ///     Gets or sets the explicit agent/persona identifier used to execute the node.
    /// </summary>
    public string? Agent { get; set; }

    /// <summary>
    ///     Gets or sets the preferred backend model identifiers for the node.
    /// </summary>
    public IReadOnlyList<string> Models { get; set; } = [];

    /// <summary>
    ///     Gets or sets the reasoning-effort preference for the selected model.
    ///     Valid values are <c>low</c>, <c>medium</c>, <c>high</c>, <c>xhigh</c>, and <c>highest-supported</c>.
    /// </summary>
    public string? ReasoningEffort { get; set; }

    /// <summary>
    ///     Gets or sets the optional per-node response timeout using the .NET <see cref="System.TimeSpan" /> string format.
    /// </summary>
    public string? ResponseTimeout { get; set; }

    /// <summary>
    ///     Gets or sets the static assets used by the node.
    /// </summary>
    public IReadOnlyList<WorkflowAssetReference> Assets { get; set; } = [];

    /// <summary>
    ///     Gets or sets the attachment source references used by the node.
    /// </summary>
    public IReadOnlyList<string> Inputs { get; set; } = [];

    /// <summary>
    ///     Gets or sets the declared output names produced by the node.
    /// </summary>
    public IReadOnlyList<string> Outputs { get; set; } = [];

    /// <summary>
    ///     Gets or sets the optional custom message appended to the prompt.
    /// </summary>
    public string? CustomMessage { get; set; }

    /// <summary>
    ///     Gets or sets the next node identifier for linear continuation.
    /// </summary>
    public string? Next { get; set; }

    /// <summary>
    ///     Gets or sets the wait duration for a wait node using the .NET <see cref="System.TimeSpan" /> string format.
    /// </summary>
    public string? WaitDuration { get; set; }

    /// <summary>
    ///     Gets or sets the human-readable reason recorded when a wait node pauses the workflow.
    /// </summary>
    public string? WaitReason { get; set; }

    /// <summary>
    ///     Gets or sets the decision resolution mode.
    /// </summary>
    public WorkflowDecisionMode DecisionMode { get; set; } = WorkflowDecisionMode.Rules;

    /// <summary>
    ///     Gets or sets the rule set identifier used for rules-based decisions.
    /// </summary>
    public string? RuleSet { get; set; }

    /// <summary>
    ///     Gets or sets the source node identifier whose output should be evaluated by the decision.
    /// </summary>
    public string? DecisionSourceNodeId { get; set; }

    /// <summary>
    ///     Gets or sets the available decision choices.
    /// </summary>
    public IReadOnlyList<WorkflowDecisionOptionDefinition> Choices { get; set; } = [];

    /// <summary>
    ///     Gets or sets the fork branches emitted by the node.
    /// </summary>
    public IReadOnlyList<WorkflowForkBranchDefinition> Branches { get; set; } = [];

    /// <summary>
    ///     Gets or sets the join node identifier paired with the fork.
    /// </summary>
    public string? JoinNodeId { get; set; }

    /// <summary>
    ///     Gets or sets the fork identifier paired with the join.
    /// </summary>
    public string? ForkId { get; set; }

    /// <summary>
    ///     Gets or sets the exit status produced by an exit node.
    /// </summary>
    public WorkflowRunStatus ExitStatus { get; set; } = WorkflowRunStatus.Stopped;
}
