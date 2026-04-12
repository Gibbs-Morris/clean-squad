namespace CleanSquad.Workflow.Definitions;

/// <summary>
///     Describes a named workflow entry point.
/// </summary>
public sealed class WorkflowEntryPointDefinition
{
    /// <summary>
    ///     Gets or sets the unique entry point identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the node identifier targeted by the entry point.
    /// </summary>
    public string NodeId { get; set; } = string.Empty;
}
