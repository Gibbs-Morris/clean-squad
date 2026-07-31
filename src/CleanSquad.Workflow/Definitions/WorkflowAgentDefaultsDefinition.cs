using System.Collections.Generic;

namespace CleanSquad.Workflow.Definitions;

/// <summary>
///     Defines workflow-level defaults for agent-backed node execution.
/// </summary>
public sealed class WorkflowAgentDefaultsDefinition
{
    /// <summary>
    ///     Gets or sets the ordered backend model preferences inherited by agent-backed nodes.
    /// </summary>
    public IReadOnlyList<string> Models { get; set; } = [];

    /// <summary>
    ///     Gets or sets the reasoning-effort preference inherited by agent-backed nodes.
    /// </summary>
    public string? ReasoningEffort { get; set; }

    /// <summary>
    ///     Gets or sets the response timeout inherited by agent-backed nodes using the .NET
    ///     <see cref="System.TimeSpan" /> string format.
    /// </summary>
    public string? ResponseTimeout { get; set; }
}
