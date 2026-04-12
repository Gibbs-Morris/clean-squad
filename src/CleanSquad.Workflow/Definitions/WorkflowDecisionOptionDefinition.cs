namespace CleanSquad.Workflow.Definitions;

/// <summary>
///     Describes one available choice for a workflow decision node.
/// </summary>
public sealed class WorkflowDecisionOptionDefinition
{
    /// <summary>
    ///     Gets or sets the choice identifier returned by the runtime or model.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the human-friendly label for the choice.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    ///     Gets or sets the next node identifier selected by the choice.
    /// </summary>
    public string NextNodeId { get; set; } = string.Empty;
}
