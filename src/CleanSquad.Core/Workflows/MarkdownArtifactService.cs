using System;
using System.IO;
using System.Text;

namespace CleanSquad.Core.Workflows;

/// <summary>
///     Creates, writes, and parses the markdown artifacts used by the workflow.
/// </summary>
public sealed class MarkdownArtifactService
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(false);
    private readonly TimeProvider timeProvider;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MarkdownArtifactService" /> class.
    /// </summary>
    /// <param name="timeProvider">The time provider used to create deterministic run identifiers.</param>
    public MarkdownArtifactService(TimeProvider? timeProvider = null)
    {
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    ///     Creates a new run folder and copies the source request markdown into it.
    /// </summary>
    /// <param name="repositoryRootPath">The repository root path.</param>
    /// <param name="sourceRequestPath">The original request markdown path.</param>
    /// <returns>The created workflow artifact set.</returns>
    public WorkflowArtifacts CreateRunArtifacts(string repositoryRootPath, string sourceRequestPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRequestPath);

        if (!File.Exists(sourceRequestPath))
        {
            throw new FileNotFoundException("The request markdown file could not be found.", sourceRequestPath);
        }

        WorkflowArtifacts artifacts = WorkflowArtifacts.Create(repositoryRootPath, sourceRequestPath, this.timeProvider);
        Directory.CreateDirectory(artifacts.RunDirectoryPath);

        string requestMarkdown = ReadMarkdown(sourceRequestPath);
        WriteMarkdown(artifacts.RequestMarkdownPath, requestMarkdown);

        return artifacts;
    }

    /// <summary>
    ///     Reads a markdown file and normalizes its line endings.
    /// </summary>
    /// <param name="path">The markdown file path.</param>
    /// <returns>The normalized markdown content.</returns>
    public static string ReadMarkdown(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string content = File.ReadAllText(path, Encoding.UTF8);
        return NormalizeMarkdown(content);
    }

    /// <summary>
    ///     Writes normalized markdown content to disk.
    /// </summary>
    /// <param name="path">The target path.</param>
    /// <param name="content">The markdown content to write.</param>
    public static void WriteMarkdown(string path, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(content);

        string? directoryPath = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        File.WriteAllText(path, NormalizeMarkdown(content), Utf8WithoutBom);
    }

    /// <summary>
    ///     Extracts the reviewer decision from markdown output.
    /// </summary>
    /// <param name="reviewMarkdown">The reviewer markdown content.</param>
    /// <returns>The parsed review decision.</returns>
    public static WorkflowReviewDecision ParseReviewDecision(string reviewMarkdown)
    {
        ArgumentNullException.ThrowIfNull(reviewMarkdown);

        foreach (string line in SplitLines(reviewMarkdown))
        {
            if (!line.StartsWith("Approved:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            int separatorIndex = line.AsSpan().IndexOf(':');
            string value = line[(separatorIndex + 1)..].Trim();
            bool approved = value.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || value.Equals("true", StringComparison.OrdinalIgnoreCase)
                || value.Equals("approved", StringComparison.OrdinalIgnoreCase);

            return new WorkflowReviewDecision(approved, NormalizeMarkdown(reviewMarkdown));
        }

        return new WorkflowReviewDecision(false, NormalizeMarkdown(reviewMarkdown));
    }

    /// <summary>
    ///     Writes the workflow state file.
    /// </summary>
    /// <param name="artifacts">The workflow artifact set.</param>
    /// <param name="result">The workflow result.</param>
    public static void WriteState(WorkflowArtifacts artifacts, WorkflowRunResult result)
    {
        ArgumentNullException.ThrowIfNull(artifacts);
        ArgumentNullException.ThrowIfNull(result);

        string rebuildDocument = result.RebuildPerformed ? Path.GetFileName(artifacts.RebuildMarkdownPath) : "not-created";
        string stateMarkdown = $"""
# Workflow State
Status: {result.Status}
ReviewApproved: {ToYesNo(result.ReviewApproved)}
RebuildPerformed: {ToYesNo(result.RebuildPerformed)}
RunId: {artifacts.RunId}

## Files
- request: {Path.GetFileName(artifacts.RequestMarkdownPath)}
- plan: {Path.GetFileName(artifacts.PlanMarkdownPath)}
- build: {Path.GetFileName(artifacts.BuildMarkdownPath)}
- review: {Path.GetFileName(artifacts.ReviewMarkdownPath)}
- rebuild: {rebuildDocument}
- final: {Path.GetFileName(artifacts.FinalMarkdownPath)}
""";

        WriteMarkdown(artifacts.StateMarkdownPath, stateMarkdown);
    }

    private static string NormalizeMarkdown(string content)
    {
        string normalized = content.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');

        return normalized.TrimEnd() + "\n";
    }

    private static string ToYesNo(bool value)
    {
        return value ? "yes" : "no";
    }

    private static string[] SplitLines(string content)
    {
        return content.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }
}
