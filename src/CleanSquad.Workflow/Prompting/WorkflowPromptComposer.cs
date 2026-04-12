using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CleanSquad.Workflow.Definitions;

namespace CleanSquad.Workflow.Prompting;

/// <summary>
///     Creates prompts from workflow definitions and file-based assets.
/// </summary>
public static class WorkflowPromptComposer
{
    /// <summary>
    ///     Creates the prompt for a workflow node.
    /// </summary>
    /// <param name="definition">The loaded workflow definition.</param>
    /// <param name="node">The workflow node.</param>
    /// <param name="attachmentFilePaths">The run artifact files provided as dynamic context.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>The composed node prompt.</returns>
    public static async Task<string> ComposeAsync(
        WorkflowDefinition definition,
        WorkflowNodeDefinition node,
        IReadOnlyList<string> attachmentFilePaths,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(attachmentFilePaths);

        List<WorkflowAssetReference> assets = [];
        assets.AddRange(definition.SharedAssets);
        assets.AddRange(node.Assets);
        string assetMarkdown = await WorkflowAssetLoader.LoadMarkdownBlocksAsync(assets, cancellationToken);
        string attachmentList = string.Join(Environment.NewLine, attachmentFilePaths.Select(path => $"- {Path.GetFileName(path)}"));
        string displayName = string.IsNullOrWhiteSpace(node.DisplayName) ? node.Id : node.DisplayName;
        string roleName = string.IsNullOrWhiteSpace(node.Role) ? node.Id : node.Role;
        string agentName = ResolveAgentName(node);
        string modelList = node.Models.Count == 0
            ? "- (default)"
            : string.Join(Environment.NewLine, node.Models.Select(model => $"- {model}"));
        string inputList = node.Inputs.Count == 0
            ? "- request"
            : string.Join(Environment.NewLine, node.Inputs.Select(input => $"- {input}"));
        string outputList = node.Outputs.Count == 0
            ? "- (not declared)"
            : string.Join(Environment.NewLine, node.Outputs.Select(output => $"- {output}"));
        string customMessageSection = string.IsNullOrWhiteSpace(node.CustomMessage)
            ? string.Empty
            : $"""

## Custom Stage Message
{node.CustomMessage}
""";
        string executionContract = node.Kind == WorkflowNodeKind.Decision
            ? BuildDecisionInstructions(node)
            : "Return markdown only and do not wrap the response in code fences.";

        return $"""
You are executing the '{displayName}' node for the '{definition.Name}' workflow.
Role: {roleName}
Agent: {agentName}
Use only the workflow assets and markdown context listed below.
{executionContract}

## Stage Configuration
### Inputs
{inputList}

### Declared Outputs
{outputList}

### Preferred Models
{modelList}{customMessageSection}

## Workflow Assets
{assetMarkdown}

## Run Context Files
{attachmentList}
""";
    }

    /// <summary>
    ///     Creates the prompt for a legacy workflow stage.
    /// </summary>
    /// <param name="definition">The workflow definition.</param>
    /// <param name="stage">The requested stage.</param>
    /// <param name="attachmentFilePaths">The run artifact files provided as dynamic context.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>The composed stage prompt.</returns>
    public static Task<string> ComposeAsync(
        WorkflowDefinition definition,
        WorkflowStage stage,
        IReadOnlyList<string> attachmentFilePaths,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(attachmentFilePaths);

        WorkflowNodeDefinition node = definition.Nodes.Count > 0
            ? stage switch
            {
                WorkflowStage.Planner => GetNodeDefinition(definition, "planner"),
                WorkflowStage.Builder => GetNodeDefinition(definition, "builder"),
                WorkflowStage.Reviewer => GetNodeDefinition(definition, "reviewer"),
                WorkflowStage.Decision => GetNodeDefinition(definition, "review-decision"),
                WorkflowStage.Rebuilder => GetNodeDefinition(definition, "rebuilder"),
                _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unsupported workflow stage."),
            }
            : CreateLegacyNodeDefinition(definition, stage);

        return ComposeAsync(definition, node, attachmentFilePaths, cancellationToken);
    }

    /// <summary>
    ///     Gets the configured workflow node.
    /// </summary>
    /// <param name="definition">The workflow definition.</param>
    /// <param name="nodeId">The requested node identifier.</param>
    /// <returns>The matching node definition.</returns>
    public static WorkflowNodeDefinition GetNodeDefinition(WorkflowDefinition definition, string nodeId)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);

        return definition.Nodes.FirstOrDefault(node => string.Equals(node.Id, nodeId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"The workflow definition does not configure the '{nodeId}' node.");
    }

    private static WorkflowNodeDefinition CreateLegacyNodeDefinition(WorkflowDefinition definition, WorkflowStage stage)
    {
        WorkflowStageDefinition stageDefinition = stage switch
        {
            WorkflowStage.Planner => definition.Planner ?? throw MissingStage(stage),
            WorkflowStage.Builder => definition.Builder ?? throw MissingStage(stage),
            WorkflowStage.Reviewer => definition.Reviewer ?? throw MissingStage(stage),
            WorkflowStage.Decision => definition.Decision ?? throw MissingStage(stage),
            WorkflowStage.Rebuilder => definition.Rebuilder ?? throw MissingStage(stage),
            _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unsupported workflow stage."),
        };

        return new WorkflowNodeDefinition
        {
            Id = stage.ToString(),
            Kind = stage == WorkflowStage.Decision ? WorkflowNodeKind.Decision : WorkflowNodeKind.Stage,
            DisplayName = stageDefinition.DisplayName ?? stage.ToString(),
            Role = stageDefinition.Role ?? stage.ToString(),
            Agent = stageDefinition.Agent,
            Models = stageDefinition.Models,
            Inputs = stageDefinition.Inputs,
            Outputs = stageDefinition.Outputs,
            CustomMessage = stageDefinition.CustomMessage,
            Assets = stageDefinition.Assets,
        };
    }

    private static string ResolveAgentName(WorkflowNodeDefinition node)
    {
        if (!string.IsNullOrWhiteSpace(node.Agent))
        {
            return node.Agent;
        }

        return string.IsNullOrWhiteSpace(node.Role) ? node.Id : node.Role;
    }

    private static string BuildDecisionInstructions(WorkflowNodeDefinition node)
    {
        string choiceList = node.Choices.Count == 0
            ? "- none configured"
            : string.Join(Environment.NewLine, node.Choices.Select(choice => $"- {choice.Id}: {choice.DisplayName ?? choice.Id}"));
        return $"""
Choose exactly one decision option and write `Choice: <option-id>` on its own line.

## Available Choices
{choiceList}
""";
    }

    private static InvalidOperationException MissingStage(WorkflowStage stage)
    {
        return new InvalidOperationException($"The workflow definition does not configure the {stage} stage.");
    }
}
