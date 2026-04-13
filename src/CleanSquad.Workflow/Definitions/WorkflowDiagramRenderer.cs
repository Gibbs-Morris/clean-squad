using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CleanSquad.Workflow.Definitions;

/// <summary>
///     Renders workflow definitions as Mermaid diagrams wrapped in markdown.
/// </summary>
public static class WorkflowDiagramRenderer
{
    /// <summary>
    ///     Creates a markdown document containing a Mermaid diagram for the workflow definition.
    /// </summary>
    /// <param name="definition">The workflow definition.</param>
    /// <param name="workflowDefinitionPath">The source workflow definition path.</param>
    /// <returns>The markdown document.</returns>
    public static string RenderMarkdown(WorkflowDefinition definition, string workflowDefinitionPath)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowDefinitionPath);

        string normalizedDefinitionPath = Path.GetFullPath(workflowDefinitionPath);
        string mermaid = RenderMermaid(definition);
        string defaultEntryPoint = string.IsNullOrWhiteSpace(definition.DefaultEntryPoint)
            ? "(not set)"
            : definition.DefaultEntryPoint;

        return $"""
# Workflow Diagram

- Name: {definition.Name}
- Definition: {normalizedDefinitionPath}
- Default entry point: {defaultEntryPoint}

```mermaid
{mermaid}
```
""";
    }

    /// <summary>
    ///     Creates Mermaid flowchart markup for the workflow definition.
    /// </summary>
    /// <param name="definition">The workflow definition.</param>
    /// <returns>The Mermaid flowchart markup.</returns>
    public static string RenderMermaid(WorkflowDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        Dictionary<string, string> mermaidIdsByNodeId = definition.Nodes
            .Select((node, index) => new { node.Id, MermaidId = $"node_{index}" })
            .ToDictionary(item => item.Id, item => item.MermaidId, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> mermaidIdsByEntryPointId = definition.EntryPoints
            .Select((entryPoint, index) => new { entryPoint.Id, MermaidId = $"entry_{index}" })
            .ToDictionary(item => item.Id, item => item.MermaidId, StringComparer.OrdinalIgnoreCase);

        StringBuilder builder = new();
        builder.AppendLine("flowchart TD");

        for (int entryPointIndex = 0; entryPointIndex < definition.EntryPoints.Count; entryPointIndex++)
        {
            WorkflowEntryPointDefinition entryPoint = definition.EntryPoints[entryPointIndex];
            string mermaidId = mermaidIdsByEntryPointId[entryPoint.Id];
            builder.Append("    ")
                .Append(mermaidId)
                .Append("([\"")
                .Append(EscapeMermaidLabel($"Entry: {entryPoint.Id}"))
                .Append("\"])")
                .AppendLine();
        }

        foreach (WorkflowNodeDefinition node in definition.Nodes)
        {
            builder.Append("    ")
                .Append(mermaidIdsByNodeId[node.Id])
                .Append(BuildNodeShape(node))
                .AppendLine();
        }

        builder.AppendLine();
        for (int entryPointIndex = 0; entryPointIndex < definition.EntryPoints.Count; entryPointIndex++)
        {
            WorkflowEntryPointDefinition entryPoint = definition.EntryPoints[entryPointIndex];
            if (mermaidIdsByNodeId.TryGetValue(entryPoint.NodeId, out string? targetMermaidId))
            {
                builder.Append("    ")
                    .Append(mermaidIdsByEntryPointId[entryPoint.Id])
                    .Append(" --> ")
                    .Append(targetMermaidId)
                    .AppendLine();
            }
        }

        foreach (WorkflowNodeDefinition node in definition.Nodes)
        {
            string sourceMermaidId = mermaidIdsByNodeId[node.Id];
            switch (node.Kind)
            {
                case WorkflowNodeKind.Stage:
                case WorkflowNodeKind.Wait:
                case WorkflowNodeKind.Join:
                    AppendEdge(builder, sourceMermaidId, node.Next, mermaidIdsByNodeId, null);
                    break;

                case WorkflowNodeKind.Decision:
                    foreach (WorkflowDecisionOptionDefinition choice in node.Choices)
                    {
                        AppendEdge(builder, sourceMermaidId, choice.NextNodeId, mermaidIdsByNodeId, choice.DisplayName ?? choice.Id);
                    }

                    break;

                case WorkflowNodeKind.Fork:
                    foreach (WorkflowForkBranchDefinition branch in node.Branches)
                    {
                        AppendEdge(builder, sourceMermaidId, branch.NextNodeId, mermaidIdsByNodeId, branch.Id);
                    }

                    if (!string.IsNullOrWhiteSpace(node.JoinNodeId))
                    {
                        AppendDottedEdge(builder, sourceMermaidId, node.JoinNodeId, mermaidIdsByNodeId, "join");
                    }

                    break;
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static string BuildNodeShape(WorkflowNodeDefinition node)
    {
        string displayName = string.IsNullOrWhiteSpace(node.DisplayName) ? node.Id : node.DisplayName;
        string label = EscapeMermaidLabel($"{displayName}\\n[{node.Kind}]\\n{node.Id}");

        return node.Kind switch
        {
            WorkflowNodeKind.Decision => $"{{\"{label}\"}}",
            WorkflowNodeKind.Exit => $"([\"{label}\"])",
            _ => $"[\"{label}\"]",
        };
    }

    private static void AppendEdge(
        StringBuilder builder,
        string sourceMermaidId,
        string? targetNodeId,
        Dictionary<string, string> mermaidIdsByNodeId,
        string? label)
    {
        if (string.IsNullOrWhiteSpace(targetNodeId) || !mermaidIdsByNodeId.TryGetValue(targetNodeId, out string? targetMermaidId))
        {
            return;
        }

        builder.Append("    ")
            .Append(sourceMermaidId);
        if (string.IsNullOrWhiteSpace(label))
        {
            builder.Append(" --> ");
        }
        else
        {
            builder.Append(" -- \"")
                .Append(EscapeMermaidLabel(label))
                .Append("\" --> ");
        }

        builder.Append(targetMermaidId)
            .AppendLine();
    }

    private static void AppendDottedEdge(
        StringBuilder builder,
        string sourceMermaidId,
        string? targetNodeId,
        Dictionary<string, string> mermaidIdsByNodeId,
        string label)
    {
        if (string.IsNullOrWhiteSpace(targetNodeId) || !mermaidIdsByNodeId.TryGetValue(targetNodeId, out string? targetMermaidId))
        {
            return;
        }

        builder.Append("    ")
            .Append(sourceMermaidId)
            .Append(" -. \"")
            .Append(EscapeMermaidLabel(label))
            .Append("\" .-> ")
            .Append(targetMermaidId)
            .AppendLine();
    }

    private static string EscapeMermaidLabel(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
    }
}
