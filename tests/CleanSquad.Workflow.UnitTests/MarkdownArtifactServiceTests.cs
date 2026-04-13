using System;
using System.IO;
using System.Linq;
using CleanSquad.Workflow;
using CleanSquad.Workflow.Decisions;
using CleanSquad.Workflow.Orchestration;
using CleanSquad.Workflow.Storage;
using CleanSquad.Workflow.UnitTests.TestFixtures;

namespace CleanSquad.Workflow.UnitTests;

/// <summary>
///     Unit tests for <see cref="MarkdownArtifactService" />.
/// </summary>
public sealed class MarkdownArtifactServiceTests
{
    /// <summary>
    ///     Verifies the service copies the workflow and request files into the run folder.
    /// </summary>
    [Fact]
    public void CreateRunArtifactsCopiesWorkflowAndRequestFiles()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            string workflowDefinitionPath = Path.Combine(tempDirectoryPath, "workflow.json");
            string requestPath = Path.Combine(tempDirectoryPath, "request.md");
            File.WriteAllText(workflowDefinitionPath, "{}", System.Text.Encoding.UTF8);
            File.WriteAllText(requestPath, "# Request\r\ncontent", System.Text.Encoding.UTF8);

            MarkdownArtifactService service = new(new FixedTimeProvider(new DateTimeOffset(2026, 4, 11, 13, 0, 0, TimeSpan.Zero)));
            WorkflowArtifacts artifacts = service.CreateRunArtifacts(tempDirectoryPath, workflowDefinitionPath, requestPath);

            Assert.Equal(
                Path.Combine(tempDirectoryPath, ".workflow-testing", "workflow-runs", "20260411-130000-request"),
                artifacts.RunDirectoryPath);
            Assert.Equal("{}", File.ReadAllText(artifacts.WorkflowDefinitionPath));
            Assert.Equal("# Request\ncontent\n", File.ReadAllText(artifacts.RequestMarkdownPath));
        }
        finally
        {
            Directory.Delete(tempDirectoryPath, true);
        }
    }

    /// <summary>
    ///     Verifies the state file records workflow decisions.
    /// </summary>
    [Fact]
    public void WriteStateIncludesDecisions()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            WorkflowArtifacts artifacts = WorkflowArtifacts.Create(
                tempDirectoryPath,
                Path.Combine(tempDirectoryPath, "workflow.json"),
                Path.Combine(tempDirectoryPath, "request.md"),
                TimeProvider.System);
            Directory.CreateDirectory(artifacts.RunDirectoryPath);
            MarkdownArtifactService service = new();
            WorkflowRunState state = WorkflowRunState.Create(artifacts.RunId, "Test Workflow", "planner", TimeProvider.System);
            state.Status = WorkflowRunStatus.Stopped;
            state.ExitNodeId = "stopped";
            state.CompletedAtUtc = state.StartedAtUtc;
            state.Decisions.Add(new WorkflowDecision(WorkflowDecisionAction.Stop, "Limit reached.", "review-rules", "# Review", "stop", "stopped"));

            service.WriteState(artifacts, state);

            string stateMarkdown = File.ReadAllText(artifacts.StateMarkdownPath);
            Assert.Contains("Status: Stopped", stateMarkdown, StringComparison.Ordinal);
            Assert.Contains("Stop (review-rules)", stateMarkdown, StringComparison.Ordinal);
            Assert.True(File.Exists(artifacts.StateJsonPath));
        }
        finally
        {
            Directory.Delete(tempDirectoryPath, true);
        }
    }

    /// <summary>
    ///     Verifies the state markdown records active waiting nodes for paused workflows.
    /// </summary>
    [Fact]
    public void WriteStateIncludesWaitingNodes()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            FixedTimeProvider timeProvider = new(new DateTimeOffset(2026, 4, 13, 10, 0, 0, TimeSpan.Zero));
            WorkflowArtifacts artifacts = WorkflowArtifacts.Create(
                tempDirectoryPath,
                Path.Combine(tempDirectoryPath, "workflow.json"),
                Path.Combine(tempDirectoryPath, "request.md"),
                timeProvider);
            Directory.CreateDirectory(artifacts.RunDirectoryPath);
            MarkdownArtifactService service = new(timeProvider);
            WorkflowRunState state = WorkflowRunState.Create(artifacts.RunId, "Test Workflow", "builder", timeProvider);
            state.Status = WorkflowRunStatus.Paused;
            state.PendingActivations.Clear();
            state.WaitingNodes.Add(new WorkflowWaitState
            {
                NodeId = "wait-for-ci",
                NextNodeId = "github-poll",
                WaitDuration = "00:05:00",
                Reason = "Wait for CI checks to finish.",
                WaitStartedAtUtc = timeProvider.GetUtcNow(),
                WaitUntilUtc = timeProvider.GetUtcNow().AddMinutes(5),
            });

            service.WriteState(artifacts, state);

            string stateMarkdown = File.ReadAllText(artifacts.StateMarkdownPath);
            Assert.Contains("Status: Paused", stateMarkdown, StringComparison.Ordinal);
            Assert.Contains("## Waiting Nodes", stateMarkdown, StringComparison.Ordinal);
            Assert.Contains("wait-for-ci", stateMarkdown, StringComparison.Ordinal);
            Assert.Contains("github-poll", stateMarkdown, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(tempDirectoryPath, true);
        }
    }

    private static string CreateTempDirectory()
    {
        string tempDirectoryPath = Path.Combine(Path.GetTempPath(), $"clean-squad-workflow-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectoryPath);
        return tempDirectoryPath;
    }
}
