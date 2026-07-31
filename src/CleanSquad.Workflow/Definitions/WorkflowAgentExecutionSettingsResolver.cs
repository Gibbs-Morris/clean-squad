using System;

namespace CleanSquad.Workflow.Definitions;

/// <summary>
///     Resolves effective model execution settings from workflow defaults and node overrides.
/// </summary>
public static class WorkflowAgentExecutionSettingsResolver
{
    /// <summary>
    ///     Resolves the effective model execution settings for an agent-backed workflow node.
    /// </summary>
    /// <param name="definition">The containing workflow definition.</param>
    /// <param name="node">The node whose settings should be resolved.</param>
    /// <returns>The effective settings, or empty settings for a node that does not execute an agent.</returns>
    public static WorkflowAgentExecutionSettings Resolve(
        WorkflowDefinition definition,
        WorkflowNodeDefinition node)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(node);

        if (!ExecutesAgent(node))
        {
            return new WorkflowAgentExecutionSettings();
        }

        WorkflowAgentDefaultsDefinition defaults = definition.AgentDefaults ?? new WorkflowAgentDefaultsDefinition();
        bool inheritDefaults = node.InheritAgentDefaults;

        return new WorkflowAgentExecutionSettings
        {
            Models = node.Models.Count > 0 || !inheritDefaults ? node.Models : defaults.Models,
            ReasoningEffort = !string.IsNullOrWhiteSpace(node.ReasoningEffort) || !inheritDefaults
                ? node.ReasoningEffort
                : defaults.ReasoningEffort,
            ResponseTimeout = !string.IsNullOrWhiteSpace(node.ResponseTimeout) || !inheritDefaults
                ? node.ResponseTimeout
                : defaults.ResponseTimeout,
        };
    }

    private static bool ExecutesAgent(WorkflowNodeDefinition node)
    {
        return node.Kind == WorkflowNodeKind.Stage
            || (node.Kind == WorkflowNodeKind.Decision && node.DecisionMode == WorkflowDecisionMode.Agent);
    }
}
