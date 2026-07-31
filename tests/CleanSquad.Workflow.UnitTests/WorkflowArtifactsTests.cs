using System;
using System.IO;
using CleanSquad.Workflow.Storage;
using CleanSquad.Workflow.UnitTests.TestFixtures;

namespace CleanSquad.Workflow.UnitTests;

/// <summary>
///     Unit tests for <see cref="WorkflowArtifacts" />.
/// </summary>
public sealed class WorkflowArtifactsTests
{
    /// <summary>
    ///     Verifies the artifact paths are generated deterministically.
    /// </summary>
    [Fact]
    public void CreateUsesDeterministicRunPaths()
    {
        string workspaceRootPath = Path.Combine(Path.GetTempPath(), "workspace");
        WorkflowArtifacts artifacts = WorkflowArtifacts.Create(
            workspaceRootPath,
            Path.Combine(workspaceRootPath, "workflow-definitions", "default", "workflow.json"),
            Path.Combine(workspaceRootPath, "workflow-demo", "request.md"),
            new FixedTimeProvider(new DateTimeOffset(2026, 4, 11, 12, 0, 0, TimeSpan.Zero)));

        Assert.Equal("20260411-120000-request", artifacts.RunId);
        Assert.EndsWith(
            Path.Combine(".workflow-testing", "workflow-runs", "20260411-120000-request"),
            artifacts.RunDirectoryPath,
            StringComparison.Ordinal);
        Assert.EndsWith("build-01.md", artifacts.GetBuildMarkdownPath(1), StringComparison.Ordinal);
        Assert.EndsWith("review-02.md", artifacts.GetReviewMarkdownPath(2), StringComparison.Ordinal);
        Assert.EndsWith("decision-02.md", artifacts.GetDecisionMarkdownPath(2), StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies artifact paths honor configured storage directory names.
    /// </summary>
    [Fact]
    public void CreateUsesConfiguredStorageDirectories()
    {
        string workspaceRootPath = Path.Combine(Path.GetTempPath(), "workspace");
        WorkflowArtifacts artifacts = WorkflowArtifacts.Create(
            workspaceRootPath,
            Path.Combine(workspaceRootPath, "workflow-definitions", "default", "workflow.json"),
            Path.Combine(workspaceRootPath, "workflow-demo", "request.md"),
            new FixedTimeProvider(new DateTimeOffset(2026, 4, 11, 12, 0, 0, TimeSpan.Zero)),
            new WorkflowStorageOptions
            {
                WorkflowTestingDirectoryName = ".runtime-data",
                WorkflowRunsDirectoryName = "runs",
            });

        Assert.EndsWith(
            Path.Combine(".runtime-data", "runs", "20260411-120000-request"),
            artifacts.RunDirectoryPath,
            StringComparison.Ordinal);
    }
}
