namespace CleanSquad.Workflow.Definitions;

/// <summary>
///     Describes one branch emitted by a workflow fork node.
/// </summary>
public sealed class WorkflowForkBranchDefinition
{
    /// <summary>
    ///     Gets or sets the branch identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the first node identifier for the branch.
    /// </summary>
    public string NextNodeId { get; set; } = string.Empty;
}
