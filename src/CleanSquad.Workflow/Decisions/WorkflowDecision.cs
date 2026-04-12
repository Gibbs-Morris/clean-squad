namespace CleanSquad.Workflow.Decisions;

/// <summary>
///     Represents a single decision taken during workflow execution.
/// </summary>
/// <param name="Action">The selected next action.</param>
/// <param name="Reason">A short human-readable reason for the decision.</param>
/// <param name="Source">The source that produced the decision.</param>
/// <param name="RawContent">The raw markdown content used to derive the decision.</param>
/// <param name="ChoiceId">The selected choice identifier.</param>
/// <param name="NextNodeId">The next node selected by the decision.</param>
public sealed record WorkflowDecision(
    WorkflowDecisionAction Action,
    string Reason,
    string Source,
    string RawContent,
    string? ChoiceId = null,
    string? NextNodeId = null);
