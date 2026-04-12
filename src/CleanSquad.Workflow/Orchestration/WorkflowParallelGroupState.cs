using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace CleanSquad.Workflow.Orchestration;

/// <summary>
///     Tracks branch completion for one fork/join parallel group.
/// </summary>
[JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
public sealed class WorkflowParallelGroupState
{
    /// <summary>
    ///     Gets or sets the group identifier.
    /// </summary>
    public string GroupId { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the originating fork node identifier.
    /// </summary>
    public string ForkNodeId { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the paired join node identifier.
    /// </summary>
    public string JoinNodeId { get; set; } = string.Empty;

    /// <summary>
    ///     Gets the expected branch identifiers.
    /// </summary>
    public Collection<string> ExpectedBranchIds { get; } = [];

    /// <summary>
    ///     Gets the completed branch identifiers.
    /// </summary>
    public Collection<string> CompletedBranchIds { get; } = [];

    /// <summary>
    ///     Gets or sets a value indicating whether the join has already been released.
    /// </summary>
    public bool JoinReleased { get; set; }
}
