using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace CleanSquad.Workflow.Storage;

/// <summary>
///     Describes the file artifacts produced for a workflow run.
/// </summary>
public sealed record WorkflowArtifacts(
    string WorkspaceRootPath,
    string RunId,
    string RunDirectoryPath,
    string SourceWorkflowDefinitionPath,
    string WorkflowDefinitionPath,
    string SourceRequestPath,
    string RequestMarkdownPath,
    string FinalMarkdownPath,
    string StateMarkdownPath)
{
    /// <summary>
    ///     Creates a new set of workflow artifact paths.
    /// </summary>
    /// <param name="workspaceRootPath">The workspace root path.</param>
    /// <param name="workflowDefinitionPath">The workflow definition path.</param>
    /// <param name="sourceRequestPath">The request markdown path.</param>
    /// <param name="timeProvider">The time provider used to create the run identifier.</param>
    /// <param name="storageOptions">The optional workflow storage options used to resolve durable paths.</param>
    /// <returns>The artifact path set for the workflow run.</returns>
    public static WorkflowArtifacts Create(
        string workspaceRootPath,
        string workflowDefinitionPath,
        string sourceRequestPath,
        TimeProvider timeProvider,
        WorkflowStorageOptions? storageOptions = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowDefinitionPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRequestPath);
        ArgumentNullException.ThrowIfNull(timeProvider);

        string normalizedWorkspaceRoot = Path.GetFullPath(workspaceRootPath);
        string normalizedWorkflowDefinitionPath = Path.GetFullPath(workflowDefinitionPath);
        string normalizedSourceRequestPath = Path.GetFullPath(sourceRequestPath);
        WorkflowStorageOptions effectiveStorageOptions = storageOptions ?? new WorkflowStorageOptions();
        string slug = Slugify(Path.GetFileNameWithoutExtension(normalizedSourceRequestPath));
        string timestamp = timeProvider.GetUtcNow().ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        string runId = $"{timestamp}-{slug}";
        string runDirectoryPath = Path.Combine(effectiveStorageOptions.GetWorkflowRunsRootPath(normalizedWorkspaceRoot), runId);

        return new WorkflowArtifacts(
            normalizedWorkspaceRoot,
            runId,
            runDirectoryPath,
            normalizedWorkflowDefinitionPath,
            Path.Combine(runDirectoryPath, "workflow.json"),
            normalizedSourceRequestPath,
            Path.Combine(runDirectoryPath, "request.md"),
            Path.Combine(runDirectoryPath, "final.md"),
            Path.Combine(runDirectoryPath, "state.md"));
    }

    /// <summary>
    ///     Loads the artifact paths for an existing run folder.
    /// </summary>
    /// <param name="runPathOrStatePath">The run directory path or state file path.</param>
    /// <returns>The loaded artifact set.</returns>
    public static WorkflowArtifacts LoadExisting(string runPathOrStatePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runPathOrStatePath);

        string normalizedPath = Path.GetFullPath(runPathOrStatePath);
        string runDirectoryPath = File.Exists(normalizedPath)
            ? Path.GetDirectoryName(normalizedPath) ?? throw new InvalidOperationException("The provided state path does not have a directory.")
            : normalizedPath;
        DirectoryInfo runDirectory = new(runDirectoryPath);
        DirectoryInfo? testingDirectory = runDirectory.Parent?.Parent;
        string workspaceRootPath = testingDirectory is not null && string.Equals(testingDirectory.Name, ".workflow-testing", StringComparison.OrdinalIgnoreCase)
            ? testingDirectory.Parent?.FullName ?? runDirectory.FullName
            : runDirectory.FullName;

        return new WorkflowArtifacts(
            workspaceRootPath,
            runDirectory.Name,
            runDirectory.FullName,
            Path.Combine(runDirectory.FullName, "workflow.json"),
            Path.Combine(runDirectory.FullName, "workflow.json"),
            Path.Combine(runDirectory.FullName, "request.md"),
            Path.Combine(runDirectory.FullName, "request.md"),
            Path.Combine(runDirectory.FullName, "final.md"),
            Path.Combine(runDirectory.FullName, "state.md"));
    }

    /// <summary>
    ///     Gets the persisted JSON state path.
    /// </summary>
    public string StateJsonPath => Path.Combine(this.RunDirectoryPath, "state.json");

    /// <summary>
    ///     Gets the structured event log path.
    /// </summary>
    public string EventLogPath => Path.Combine(this.RunDirectoryPath, "events.ndjson");

    /// <summary>
    ///     Gets the step artifact directory path.
    /// </summary>
    public string StepsDirectoryPath => Path.Combine(this.RunDirectoryPath, "steps");

    /// <summary>
    ///     Gets the markdown artifact path for a deterministic step.
    /// </summary>
    /// <param name="stepNumber">The deterministic step number.</param>
    /// <param name="nodeId">The node identifier.</param>
    /// <returns>The step markdown path.</returns>
    public string GetStepMarkdownPath(int stepNumber, string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return Path.Combine(this.StepsDirectoryPath, $"{stepNumber:0000}-{Slugify(nodeId)}.md");
    }

    /// <summary>
    ///     Gets the plan artifact path.
    /// </summary>
    public string PlanMarkdownPath => Path.Combine(this.RunDirectoryPath, "plan.md");

    /// <summary>
    ///     Gets the build artifact path for a build cycle.
    /// </summary>
    /// <param name="buildNumber">The one-based build number.</param>
    /// <returns>The build artifact path.</returns>
    public string GetBuildMarkdownPath(int buildNumber)
    {
        return Path.Combine(this.RunDirectoryPath, $"build-{buildNumber:00}.md");
    }

    /// <summary>
    ///     Gets the review artifact path for a review cycle.
    /// </summary>
    /// <param name="reviewCycle">The one-based review cycle number.</param>
    /// <returns>The review artifact path.</returns>
    public string GetReviewMarkdownPath(int reviewCycle)
    {
        return Path.Combine(this.RunDirectoryPath, $"review-{reviewCycle:00}.md");
    }

    /// <summary>
    ///     Gets the decision artifact path for a review cycle.
    /// </summary>
    /// <param name="reviewCycle">The one-based review cycle number.</param>
    /// <returns>The decision artifact path.</returns>
    public string GetDecisionMarkdownPath(int reviewCycle)
    {
        return Path.Combine(this.RunDirectoryPath, $"decision-{reviewCycle:00}.md");
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
