using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace CleanSquad.Core.Workflows;

/// <summary>
///     Describes the markdown artifacts produced for a workflow run.
/// </summary>
/// <param name="RepositoryRootPath">The repository root used for the run.</param>
/// <param name="RunId">The deterministic identifier for the run folder.</param>
/// <param name="RunDirectoryPath">The folder where all markdown artifacts are written.</param>
/// <param name="SourceRequestPath">The original request markdown path supplied by the user.</param>
/// <param name="RequestMarkdownPath">The copied request markdown inside the run folder.</param>
/// <param name="PlanMarkdownPath">The planner output markdown path.</param>
/// <param name="BuildMarkdownPath">The builder output markdown path.</param>
/// <param name="ReviewMarkdownPath">The reviewer output markdown path.</param>
/// <param name="RebuildMarkdownPath">The rebuild output markdown path.</param>
/// <param name="FinalMarkdownPath">The final summary markdown path.</param>
/// <param name="StateMarkdownPath">The workflow state markdown path.</param>
public sealed record WorkflowArtifacts(
    string RepositoryRootPath,
    string RunId,
    string RunDirectoryPath,
    string SourceRequestPath,
    string RequestMarkdownPath,
    string PlanMarkdownPath,
    string BuildMarkdownPath,
    string ReviewMarkdownPath,
    string RebuildMarkdownPath,
    string FinalMarkdownPath,
    string StateMarkdownPath)
{
    /// <summary>
    ///     Creates a new set of workflow artifact paths.
    /// </summary>
    /// <param name="repositoryRootPath">The repository root path.</param>
    /// <param name="sourceRequestPath">The source request markdown path.</param>
    /// <param name="timeProvider">The time provider used to create the run identifier.</param>
    /// <returns>The artifact path set for the workflow run.</returns>
    public static WorkflowArtifacts Create(string repositoryRootPath, string sourceRequestPath, TimeProvider timeProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRequestPath);
        ArgumentNullException.ThrowIfNull(timeProvider);

        string normalizedRepositoryRoot = Path.GetFullPath(repositoryRootPath);
        string normalizedSourceRequestPath = Path.GetFullPath(sourceRequestPath);
        string slug = Slugify(Path.GetFileNameWithoutExtension(normalizedSourceRequestPath));
        string timestamp = timeProvider.GetUtcNow().ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        string runId = $"{timestamp}-{slug}";
        string runDirectoryPath = Path.Combine(normalizedRepositoryRoot, "workflow-runs", runId);

        return new WorkflowArtifacts(
            normalizedRepositoryRoot,
            runId,
            runDirectoryPath,
            normalizedSourceRequestPath,
            Path.Combine(runDirectoryPath, "request.md"),
            Path.Combine(runDirectoryPath, "plan.md"),
            Path.Combine(runDirectoryPath, "build.md"),
            Path.Combine(runDirectoryPath, "review.md"),
            Path.Combine(runDirectoryPath, "rebuild.md"),
            Path.Combine(runDirectoryPath, "final.md"),
            Path.Combine(runDirectoryPath, "state.md"));
    }

    private static string Slugify(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "request";
        }

        StringBuilder builder = new();
        bool previousWasSeparator = false;

        foreach (char character in value.Trim())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousWasSeparator = false;
                continue;
            }

            if (previousWasSeparator)
            {
                continue;
            }

            builder.Append('-');
            previousWasSeparator = true;
        }

        string slug = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "request" : slug;
    }
}
