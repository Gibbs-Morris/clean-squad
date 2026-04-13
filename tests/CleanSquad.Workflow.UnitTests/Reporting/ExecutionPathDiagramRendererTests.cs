using System.Collections.Generic;
using CleanSquad.Workflow.Orchestration;
using CleanSquad.Workflow.Reporting;

namespace CleanSquad.Workflow.UnitTests.Reporting;

/// <summary>
///     Unit tests for <see cref="ExecutionPathDiagramRenderer" />.
/// </summary>
public sealed class ExecutionPathDiagramRendererTests
{
    /// <summary>
    ///     Verifies that an empty step sequence produces a diagram with a sentinel node and no edges.
    /// </summary>
    [Fact]
    public void RenderWithEmptyStepsReturnsDiagramWithNoStepsNode()
    {
        string result = ExecutionPathDiagramRenderer.Render([]);

        Assert.Contains("no_steps[\"No steps recorded\"]", result, System.StringComparison.Ordinal);
        Assert.DoesNotContain("-->", result, System.StringComparison.Ordinal);
        Assert.Contains("```mermaid", result, System.StringComparison.Ordinal);
        Assert.Contains("# Execution Path", result, System.StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies that a single-step run produces one node declaration and no edges.
    /// </summary>
    [Fact]
    public void RenderWithSingleStepReturnsDiagramWithOneNodeAndNoEdges()
    {
        IEnumerable<WorkflowStepState> steps =
        [
            new WorkflowStepState { StepNumber = 1, NodeId = "node-a" },
        ];

        string result = ExecutionPathDiagramRenderer.Render(steps);

        Assert.Contains("node_a[\"node-a\"]", result, System.StringComparison.Ordinal);
        Assert.DoesNotContain("-->", result, System.StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies that a linear three-step run produces three nodes and two ordered edges.
    /// </summary>
    [Fact]
    public void RenderWithLinearStepsReturnsDiagramWithEdgesInOrder()
    {
        IEnumerable<WorkflowStepState> steps =
        [
            new WorkflowStepState { StepNumber = 1, NodeId = "node-a" },
            new WorkflowStepState { StepNumber = 2, NodeId = "node-b" },
            new WorkflowStepState { StepNumber = 3, NodeId = "node-c" },
        ];

        string result = ExecutionPathDiagramRenderer.Render(steps);

        Assert.Contains("node_a[\"node-a\"]", result, System.StringComparison.Ordinal);
        Assert.Contains("node_b[\"node-b\"]", result, System.StringComparison.Ordinal);
        Assert.Contains("node_c[\"node-c\"]", result, System.StringComparison.Ordinal);
        Assert.Contains("node_a --> node_b", result, System.StringComparison.Ordinal);
        Assert.Contains("node_b --> node_c", result, System.StringComparison.Ordinal);
        Assert.DoesNotContain("_2", result, System.StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies that a revisited node receives a numeric suffix and the repeat edge is drawn correctly.
    /// </summary>
    [Fact]
    public void RenderWithRevisitedNodeAppliesSuffixAndDrawsRepeatEdge()
    {
        IEnumerable<WorkflowStepState> steps =
        [
            new WorkflowStepState { StepNumber = 1, NodeId = "node-a" },
            new WorkflowStepState { StepNumber = 2, NodeId = "node-b" },
            new WorkflowStepState { StepNumber = 3, NodeId = "node-a" },
        ];

        string result = ExecutionPathDiagramRenderer.Render(steps);

        Assert.Contains("node_a[\"node-a\"]", result, System.StringComparison.Ordinal);
        Assert.Contains("node_a_2[\"node-a (2)\"]", result, System.StringComparison.Ordinal);
        Assert.Contains("node_a --> node_b", result, System.StringComparison.Ordinal);
        Assert.Contains("node_b --> node_a_2", result, System.StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies that a third visit to a node produces a <c>_3</c> suffix.
    /// </summary>
    [Fact]
    public void RenderWithThreeVisitsAppliesCorrectSuffix()
    {
        IEnumerable<WorkflowStepState> steps =
        [
            new WorkflowStepState { StepNumber = 1, NodeId = "node-a" },
            new WorkflowStepState { StepNumber = 2, NodeId = "node-b" },
            new WorkflowStepState { StepNumber = 3, NodeId = "node-a" },
            new WorkflowStepState { StepNumber = 4, NodeId = "node-b" },
        ];

        string result = ExecutionPathDiagramRenderer.Render(steps);

        Assert.Contains("node_b_2[\"node-b (2)\"]", result, System.StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies that hyphens are replaced with underscores in the Mermaid ID while the original
    ///     node identifier is preserved in the label text.
    /// </summary>
    [Fact]
    public void RenderWithHyphenatedNodeIdEscapesIdAndPreservesLabel()
    {
        IEnumerable<WorkflowStepState> steps =
        [
            new WorkflowStepState { StepNumber = 1, NodeId = "story-selection" },
        ];

        string result = ExecutionPathDiagramRenderer.Render(steps);

        Assert.Contains("story_selection[\"story-selection\"]", result, System.StringComparison.Ordinal);
        Assert.DoesNotContain("story-selection[", result, System.StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies that the output begins with a <c># Execution Path</c> heading and wraps exactly
    ///     one Mermaid fence, consistent with <see cref="CleanSquad.Workflow.Definitions.WorkflowDiagramRenderer" />.
    /// </summary>
    [Fact]
    public void RenderOutputFormatMatchesMermaidFenceConvention()
    {
        IEnumerable<WorkflowStepState> steps =
        [
            new WorkflowStepState { StepNumber = 1, NodeId = "node-a" },
        ];

        string result = ExecutionPathDiagramRenderer.Render(steps);

        Assert.StartsWith("# Execution Path", result, System.StringComparison.Ordinal);
        Assert.Equal(1, CountOccurrences(result, "```mermaid"));
        Assert.Equal(2, CountOccurrences(result, "```"));
    }

    private static int CountOccurrences(string source, string value)
    {
        int count = 0;
        int index = 0;
        while ((index = source.IndexOf(value, index, System.StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
