using System.Collections.Generic;

namespace CleanSquad.Workflow.Definitions;

/// <summary>
///     Represents the effective model execution settings for one workflow node.
/// </summary>
public sealed class WorkflowAgentExecutionSettings
{
    /// <summary>
    ///     Gets or initializes the ordered backend model preferences.
    /// </summary>
    public IReadOnlyList<string> Models { get; init; } = [];

    /// <summary>
    ///     Gets or initializes the reasoning-effort preference.
    /// </summary>
    public string? ReasoningEffort { get; init; }

    /// <summary>
    ///     Gets or initializes the response timeout using the .NET <see cref="System.TimeSpan" /> string format.
    /// </summary>
    public string? ResponseTimeout { get; init; }
}
