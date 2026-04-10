using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CleanSquad.Core.Workflows;

namespace CleanSquad.Core.UnitTests.Workflows;

/// <summary>
///     Unit tests for <see cref="WorkflowOrchestrator" />.
/// </summary>
public sealed class WorkflowOrchestratorTests
{
    /// <summary>
    ///     Verifies the workflow stops after reviewer approval.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task ExecuteAsyncStopsAfterApprovedReviewAsync()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            string requestPath = Path.Combine(tempDirectoryPath, "request.md");
            await File.WriteAllTextAsync(requestPath, "# Request\n", System.Text.Encoding.UTF8);

            FakeWorkflowAgentRunner runner = new(
            [
                "# Plan\n## Goal\nShip a small workflow\n",
                "# Build\n## Summary\nBuild summary\n",
                "# Review\nApproved: yes\n## Verdict\nLooks good.\n## Findings\n- None.\n## Builder Instructions\n- None.\n",
            ]);
            WorkflowOrchestrator orchestrator = new(
                new MarkdownArtifactService(new FixedTimeProvider(new DateTimeOffset(2026, 4, 10, 13, 0, 0, TimeSpan.Zero))),
                runner);

            WorkflowRunResult result = await orchestrator.ExecuteAsync(tempDirectoryPath, requestPath);

            Assert.Equal(WorkflowRunStatus.Approved, result.Status);
            Assert.False(result.RebuildPerformed);
            Assert.Equal(3, runner.Calls.Count);
            Assert.Equal(["request.md"], runner.Calls[0].AttachmentNames);
            Assert.Equal(["request.md", "plan.md"], runner.Calls[1].AttachmentNames);
            Assert.Equal(["request.md", "plan.md", "build.md"], runner.Calls[2].AttachmentNames);
            Assert.DoesNotContain(runner.Calls, call => call.Stage == WorkflowStage.Rebuilder);
            Assert.True(File.Exists(result.FinalArtifactPath));
            Assert.Contains(
                "Status: Approved",
                await File.ReadAllTextAsync(result.StateArtifactPath),
                StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(tempDirectoryPath, true);
        }
    }

    /// <summary>
    ///     Verifies the workflow performs one rebuild when the reviewer rejects the initial build.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task ExecuteAsyncRunsSingleRebuildWhenReviewRejectsBuildAsync()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            string requestPath = Path.Combine(tempDirectoryPath, "request.md");
            await File.WriteAllTextAsync(requestPath, "# Request\n", System.Text.Encoding.UTF8);

            FakeWorkflowAgentRunner runner = new(
            [
                "# Plan\n## Goal\nShip a small workflow\n",
                "# Build\n## Summary\nInitial build\n",
                "# Review\nApproved: no\n## Verdict\nNeeds tighter test coverage.\n## Findings\n- Missing validation details.\n## Builder Instructions\n- Add explicit validation notes.\n",
                "# Rebuild\n## Summary\nUpdated build\n## Changes Made\n- Added validation notes.\n## Remaining Risks\n- None.\n## Validation Notes\n- Added a verification section.\n",
            ]);
            WorkflowOrchestrator orchestrator = new(
                new MarkdownArtifactService(new FixedTimeProvider(new DateTimeOffset(2026, 4, 10, 13, 15, 0, TimeSpan.Zero))),
                runner);

            WorkflowRunResult result = await orchestrator.ExecuteAsync(tempDirectoryPath, requestPath);

            Assert.Equal(WorkflowRunStatus.RebuiltAfterReview, result.Status);
            Assert.True(result.RebuildPerformed);
            Assert.Equal(4, runner.Calls.Count);
            Assert.Equal(WorkflowStage.Rebuilder, runner.Calls[3].Stage);
            Assert.Equal(["request.md", "plan.md", "build.md", "review.md"], runner.Calls[3].AttachmentNames);
            Assert.Contains(
                "Updated build",
                await File.ReadAllTextAsync(result.FinalArtifactPath),
                StringComparison.Ordinal);
            Assert.Contains(
                "Status: RebuiltAfterReview",
                await File.ReadAllTextAsync(result.StateArtifactPath),
                StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(tempDirectoryPath, true);
        }
    }

    private static string CreateTempDirectory()
    {
        string tempDirectoryPath = Path.Combine(Path.GetTempPath(), $"clean-squad-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectoryPath);
        return tempDirectoryPath;
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            this.utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return this.utcNow;
        }
    }

    private sealed class FakeWorkflowAgentRunner : IWorkflowAgentRunner
    {
        private readonly Queue<string> responses;

        public FakeWorkflowAgentRunner(IEnumerable<string> responses)
        {
            this.responses = new Queue<string>(responses);
        }

        public List<AgentCall> Calls { get; } = [];

        public Task<string> RunAsync(
            WorkflowStage stage,
            string prompt,
            IReadOnlyList<string> attachmentFilePaths,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.Calls.Add(new AgentCall(stage, prompt, attachmentFilePaths.Select(filePath => Path.GetFileName(filePath) ?? string.Empty).ToArray()));

            return Task.FromResult(this.responses.Dequeue());
        }
    }

    private sealed record AgentCall(WorkflowStage Stage, string Prompt, IReadOnlyList<string> AttachmentNames);
}
