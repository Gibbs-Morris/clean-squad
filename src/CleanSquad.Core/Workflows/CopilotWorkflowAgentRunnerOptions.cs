using System;

namespace CleanSquad.Core.Workflows;

/// <summary>
///     Configures the Copilot-backed workflow agent runner.
/// </summary>
public sealed class CopilotWorkflowAgentRunnerOptions
{
    /// <summary>
    ///     Gets or sets the workspace root path used as the Copilot CLI working directory.
    /// </summary>
    public string WorkspaceRootPath { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the optional durable knowledge root for future persona memory features.
    /// </summary>
    public string? KnowledgeRootPath { get; set; }

    /// <summary>
    ///     Gets or sets the maximum time to wait for a Copilot stage response before failing the step.
    /// </summary>
    public TimeSpan? ResponseTimeout { get; set; } = TimeSpan.FromMinutes(5);
}
