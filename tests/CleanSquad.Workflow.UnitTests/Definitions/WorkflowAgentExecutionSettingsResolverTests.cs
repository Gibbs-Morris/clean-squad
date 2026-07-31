using CleanSquad.Workflow.Definitions;

namespace CleanSquad.Workflow.UnitTests.Definitions;

/// <summary>
///     Unit tests for <see cref="WorkflowAgentExecutionSettingsResolver" />.
/// </summary>
public sealed class WorkflowAgentExecutionSettingsResolverTests
{
    /// <summary>
    ///     Verifies an agent-backed stage inherits every omitted execution setting from the workflow defaults.
    /// </summary>
    [Fact]
    public void ResolveInheritsWorkflowAgentDefaults()
    {
        WorkflowDefinition definition = CreateDefinition();
        WorkflowNodeDefinition node = new()
        {
            Id = "planner",
            Kind = WorkflowNodeKind.Stage,
        };

        WorkflowAgentExecutionSettings settings =
            WorkflowAgentExecutionSettingsResolver.Resolve(definition, node);

        Assert.Equal(["model-default"], settings.Models);
        Assert.Equal(WorkflowReasoningEffort.High, settings.ReasoningEffort);
        Assert.Equal("00:10:00", settings.ResponseTimeout);
    }

    /// <summary>
    ///     Verifies node settings override inherited values independently instead of replacing the entire settings group.
    /// </summary>
    [Fact]
    public void ResolveAppliesIndependentNodeOverrides()
    {
        WorkflowDefinition definition = CreateDefinition();
        WorkflowNodeDefinition node = new()
        {
            Id = "builder",
            Kind = WorkflowNodeKind.Stage,
            Models = ["model-builder"],
            ResponseTimeout = "00:15:00",
        };

        WorkflowAgentExecutionSettings settings =
            WorkflowAgentExecutionSettingsResolver.Resolve(definition, node);

        Assert.Equal(["model-builder"], settings.Models);
        Assert.Equal(WorkflowReasoningEffort.High, settings.ReasoningEffort);
        Assert.Equal("00:15:00", settings.ResponseTimeout);
    }

    /// <summary>
    ///     Verifies a node can disable inheritance and fall back to provider and runner defaults.
    /// </summary>
    [Fact]
    public void ResolveSupportsCompleteNodeOptOut()
    {
        WorkflowDefinition definition = CreateDefinition();
        WorkflowNodeDefinition node = new()
        {
            Id = "provider-default-stage",
            Kind = WorkflowNodeKind.Stage,
            InheritAgentDefaults = false,
        };

        WorkflowAgentExecutionSettings settings =
            WorkflowAgentExecutionSettingsResolver.Resolve(definition, node);

        Assert.Empty(settings.Models);
        Assert.Null(settings.ReasoningEffort);
        Assert.Null(settings.ResponseTimeout);
    }

    /// <summary>
    ///     Verifies agent-backed decisions inherit agent defaults like stage nodes.
    /// </summary>
    [Fact]
    public void ResolveAppliesDefaultsToAgentDecision()
    {
        WorkflowDefinition definition = CreateDefinition();
        WorkflowNodeDefinition node = new()
        {
            Id = "routing-decision",
            Kind = WorkflowNodeKind.Decision,
            DecisionMode = WorkflowDecisionMode.Agent,
        };

        WorkflowAgentExecutionSettings settings =
            WorkflowAgentExecutionSettingsResolver.Resolve(definition, node);

        Assert.Equal(["model-default"], settings.Models);
    }

    /// <summary>
    ///     Verifies rules decisions do not claim inherited agent settings that are never executed.
    /// </summary>
    [Fact]
    public void ResolveDoesNotApplyDefaultsToRulesDecision()
    {
        WorkflowDefinition definition = CreateDefinition();
        WorkflowNodeDefinition node = new()
        {
            Id = "rules-decision",
            Kind = WorkflowNodeKind.Decision,
            DecisionMode = WorkflowDecisionMode.Rules,
        };

        WorkflowAgentExecutionSettings settings =
            WorkflowAgentExecutionSettingsResolver.Resolve(definition, node);

        Assert.Empty(settings.Models);
        Assert.Null(settings.ReasoningEffort);
        Assert.Null(settings.ResponseTimeout);
    }

    private static WorkflowDefinition CreateDefinition()
    {
        return new WorkflowDefinition
        {
            AgentDefaults = new WorkflowAgentDefaultsDefinition
            {
                Models = ["model-default"],
                ReasoningEffort = WorkflowReasoningEffort.High,
                ResponseTimeout = "00:10:00",
            },
        };
    }
}
