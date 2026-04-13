using System.Collections.Generic;
using System.Linq;
using System.Text;
using CleanSquad.Workflow.Orchestration;

namespace CleanSquad.Workflow.Reporting;

/// <summary>
///     Renders the executed node-visit sequence of a completed workflow run as a Mermaid flowchart.
/// </summary>
/// <remarks>
///     <para>
///         The renderer is a pure static function with no side effects. All file I/O is the caller's
///         responsibility. Callers must pass steps pre-ordered by <c>StepNumber</c> in ascending order;
///         if the input is unordered the rendered path will be incorrect but no exception will be thrown.
///     </para>
///     <para>
///         Parallel-branch ambiguity (deferred): when two branches visit the same node at the same
///         cumulative visit count, they produce identical suffixed Mermaid IDs (e.g. both produce
///         <c>node_a_2</c>). The diagram remains valid Mermaid but may be visually ambiguous for parallel
///         paths. Branch-ID annotation is deferred to a follow-on story.
///     </para>
/// </remarks>
public static class ExecutionPathDiagramRenderer
{
    /// <summary>
    ///     Renders the ordered workflow step sequence as a Mermaid <c>flowchart TD</c> diagram wrapped
    ///     in a markdown document.
    /// </summary>
    /// <param name="steps">
    ///     The ordered step sequence. Must be pre-sorted by <c>StepNumber</c> ascending. Steps with a
    ///     null or empty <c>NodeId</c> are skipped defensively.
    /// </param>
    /// <returns>
    ///     A markdown string containing a <c># Execution Path</c> heading and a fenced
    ///     <c>```mermaid</c> block. When <paramref name="steps" /> is empty, the diagram contains a
    ///     single sentinel node (<c>no_steps["No steps recorded"]</c>) and no edges. The file is always
    ///     written — silent-skip on empty input is intentionally rejected.
    /// </returns>
    public static string Render(IEnumerable<WorkflowStepState> steps)
    {
        List<WorkflowStepState> orderedSteps = (steps ?? [])
            .Where(s => !string.IsNullOrEmpty(s.NodeId))
            .ToList();

        StringBuilder mermaid = new();
        mermaid.AppendLine("flowchart TD");

        if (orderedSteps.Count == 0)
        {
            mermaid.Append("    no_steps[\"No steps recorded\"]");
            return BuildMarkdown(mermaid.ToString());
        }

        Dictionary<string, int> visitCountByNodeId = new(System.StringComparer.OrdinalIgnoreCase);
        List<string> visitIds = new(orderedSteps.Count);

        foreach (WorkflowStepState step in orderedSteps)
        {
            visitCountByNodeId.TryGetValue(step.NodeId, out int count);
            count++;
            visitCountByNodeId[step.NodeId] = count;

            string mermaidId = BuildMermaidId(step.NodeId, count);
            string label = count == 1 ? step.NodeId : $"{step.NodeId} ({count})";

            mermaid.Append("    ")
                .Append(mermaidId)
                .Append("[\"")
                .Append(EscapeLabel(label))
                .Append("\"]")
                .AppendLine();

            visitIds.Add(mermaidId);
        }

        if (visitIds.Count > 1)
        {
            mermaid.AppendLine();
            for (int i = 0; i < visitIds.Count - 1; i++)
            {
                mermaid.Append("    ")
                    .Append(visitIds[i])
                    .Append(" --> ")
                    .Append(visitIds[i + 1])
                    .AppendLine();
            }
        }

        return BuildMarkdown(mermaid.ToString().TrimEnd());
    }

    /// <summary>
    ///     Builds the Mermaid node identifier for a given node and visit number.
    /// </summary>
    /// <param name="nodeId">The canonical node identifier.</param>
    /// <param name="visitNumber">The one-based visit count for this node.</param>
    /// <returns>
    ///     The sanitised Mermaid node identifier: plain escaped ID on first visit; escaped ID with
    ///     <c>_N</c> suffix on subsequent visits.
    /// </returns>
    private static string BuildMermaidId(string nodeId, int visitNumber)
    {
        string escaped = EscapeNodeId(nodeId);
        return visitNumber == 1 ? escaped : $"{escaped}_{visitNumber}";
    }

    /// <summary>
    ///     Replaces hyphens with underscores in a node identifier to produce a valid unquoted Mermaid ID.
    /// </summary>
    /// <param name="nodeId">The canonical node identifier.</param>
    /// <returns>The Mermaid-safe identifier string.</returns>
    private static string EscapeNodeId(string nodeId) =>
        nodeId.Replace("-", "_", System.StringComparison.Ordinal);

    /// <summary>
    ///     Escapes double-quote and backslash characters in a Mermaid node label.
    /// </summary>
    /// <param name="label">The raw label text.</param>
    /// <returns>The escaped label text safe for use inside <c>"…"</c> Mermaid label delimiters.</returns>
    private static string EscapeLabel(string label) =>
        label.Replace("\\", "\\\\", System.StringComparison.Ordinal)
             .Replace("\"", "\\\"", System.StringComparison.Ordinal);

    private static string BuildMarkdown(string mermaidBody) =>
        $"""
        # Execution Path

        ```mermaid
        {mermaidBody}
        ```
        """;
}
