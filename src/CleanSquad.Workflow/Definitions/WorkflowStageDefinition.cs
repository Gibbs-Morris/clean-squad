using System.Collections.Generic;

namespace CleanSquad.Workflow.Definitions;

/// <summary>
///     Describes one configured stage in a workflow definition.
/// </summary>
public sealed class WorkflowStageDefinition
{
    /// <summary>
    ///     Gets or sets the display name shown in prompts.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    ///     Gets or sets the logical role name used when executing the stage.
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    ///     Gets or sets the agent/persona identifier used to execute the stage.
    /// </summary>
    public string? Agent { get; set; }

    /// <summary>
    ///     Gets or sets the preferred backend model identifiers for the stage.
    /// </summary>
    public IReadOnlyList<string> Models { get; set; } = [];

    /// <summary>
    ///     Gets or sets the reasoning-effort preference for the selected model.
    ///     Valid values are <c>low</c>, <c>medium</c>, <c>high</c>, <c>xhigh</c>, and <c>highest-supported</c>.
    /// </summary>
    public string? ReasoningEffort { get; set; }

    /// <summary>
    ///     Gets or sets the attachment source references used for the stage.
    /// </summary>
    public IReadOnlyList<string> Inputs { get; set; } = [];

    /// <summary>
    ///     Gets or sets the declared output names produced by the stage.
    /// </summary>
    public IReadOnlyList<string> Outputs { get; set; } = [];

    /// <summary>
    ///     Gets or sets the optional custom message appended to the stage prompt.
    /// </summary>
    public string? CustomMessage { get; set; }

    /// <summary>
    ///     Gets or sets the static assets used to shape the stage behavior.
    /// </summary>
    public IReadOnlyList<WorkflowAssetReference> Assets { get; set; } = [];
}
