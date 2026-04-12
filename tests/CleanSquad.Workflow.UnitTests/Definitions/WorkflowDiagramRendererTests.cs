using System;
using System.IO;
using CleanSquad.Workflow.Definitions;

namespace CleanSquad.Workflow.UnitTests.Definitions;

/// <summary>
///     Unit tests for <see cref="WorkflowDiagramRenderer" />.
/// </summary>
public sealed class WorkflowDiagramRendererTests
{
    /// <summary>
    ///     Verifies the renderer produces markdown containing Mermaid markup and labeled edges.
    /// </summary>
    [Fact]
    public void RenderMarkdownProducesMermaidFlowchart()
    {
        WorkflowDefinition definition = new()
        {
            Name = "Diagram Test",
            DefaultEntryPoint = "default",
            EntryPoints =
            [
                new WorkflowEntryPointDefinition { Id = "default", NodeId = "planner" },
            ],
            Nodes =
            [
                new WorkflowNodeDefinition { Id = "planner", Kind = WorkflowNodeKind.Stage, DisplayName = "Planner", Next = "review" },
                new WorkflowNodeDefinition
                {
                    Id = "review",
                    Kind = WorkflowNodeKind.Decision,
                    DisplayName = "Review",
                    Choices =
                    [
                        new WorkflowDecisionOptionDefinition { Id = "approve", DisplayName = "Approve", NextNodeId = "approved" },
                        new WorkflowDecisionOptionDefinition { Id = "rebuild", DisplayName = "Rebuild", NextNodeId = "planner" },
                    ],
                },
                new WorkflowNodeDefinition { Id = "approved", Kind = WorkflowNodeKind.Exit, DisplayName = "Approved", ExitStatus = WorkflowRunStatus.Approved },
            ],
        };

        string markdown = WorkflowDiagramRenderer.RenderMarkdown(definition, Path.Combine("C:\\temp", "workflow.json"));

        Assert.Contains("# Workflow Diagram", markdown, StringComparison.Ordinal);
        Assert.Contains("```mermaid", markdown, StringComparison.Ordinal);
        Assert.Contains("flowchart TD", markdown, StringComparison.Ordinal);
        Assert.Contains("Entry: default", markdown, StringComparison.Ordinal);
        Assert.Contains("Approve", markdown, StringComparison.Ordinal);
        Assert.Contains("Rebuild", markdown, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies the renderer includes fork branch labels and join links.
    /// </summary>
    [Fact]
    public void RenderMermaidIncludesForkBranchEdges()
    {
        WorkflowDefinition definition = new()
        {
            EntryPoints = [new WorkflowEntryPointDefinition { Id = "default", NodeId = "fork" }],
            Nodes =
            [
                new WorkflowNodeDefinition
                {
                    Id = "fork",
                    Kind = WorkflowNodeKind.Fork,
                    JoinNodeId = "join",
                    Branches =
                    [
                        new WorkflowForkBranchDefinition { Id = "code", NextNodeId = "code-stage" },
                        new WorkflowForkBranchDefinition { Id = "tests", NextNodeId = "test-stage" },
                    ],
                },
                new WorkflowNodeDefinition { Id = "code-stage", Kind = WorkflowNodeKind.Stage, Next = "join" },
                new WorkflowNodeDefinition { Id = "test-stage", Kind = WorkflowNodeKind.Stage, Next = "join" },
                new WorkflowNodeDefinition { Id = "join", Kind = WorkflowNodeKind.Join, ForkId = "fork", Next = "done" },
                new WorkflowNodeDefinition { Id = "done", Kind = WorkflowNodeKind.Exit, ExitStatus = WorkflowRunStatus.Approved },
            ],
        };

        string mermaid = WorkflowDiagramRenderer.RenderMermaid(definition);

        Assert.Contains("code", mermaid, StringComparison.Ordinal);
        Assert.Contains("tests", mermaid, StringComparison.Ordinal);
        Assert.Contains("join", mermaid, StringComparison.Ordinal);
        Assert.Contains(
            mermaid.Split(Environment.NewLine),
            line => line.Contains("-. \"join\" .->", StringComparison.Ordinal));
    }
}
