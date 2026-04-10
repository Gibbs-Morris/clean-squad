using System;
using System.IO;
using CleanSquad.Core.Workflows;

namespace CleanSquad.Core.UnitTests.Workflows;

/// <summary>
///     Unit tests for <see cref="MarkdownArtifactService" />.
/// </summary>
public sealed class MarkdownArtifactServiceTests
{
    /// <summary>
    ///     Verifies the service creates a run folder and copies the request markdown into it.
    /// </summary>
    [Fact]
    public void CreateRunArtifactsCopiesRequestMarkdownIntoRunFolder()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            string requestPath = Path.Combine(tempDirectoryPath, "input.md");
            File.WriteAllText(requestPath, "# Request\r\ncontent", System.Text.Encoding.UTF8);

            MarkdownArtifactService service = new(new FixedTimeProvider(new DateTimeOffset(2026, 4, 10, 12, 30, 45, TimeSpan.Zero)));
            WorkflowArtifacts artifacts = service.CreateRunArtifacts(tempDirectoryPath, requestPath);

            Assert.Equal(
                Path.Combine(tempDirectoryPath, "workflow-runs", "20260410-123045-input"),
                artifacts.RunDirectoryPath);
            Assert.Equal("# Request\ncontent\n", File.ReadAllText(artifacts.RequestMarkdownPath));
        }
        finally
        {
            Directory.Delete(tempDirectoryPath, true);
        }
    }

    /// <summary>
    ///     Verifies the review parser marks approved content as approved.
    /// </summary>
    [Fact]
    public void ParseReviewDecisionUsesApprovedLineWhenPresent()
    {
        WorkflowReviewDecision decision = MarkdownArtifactService.ParseReviewDecision("# Review\nApproved: yes\n");

        Assert.True(decision.Approved);
    }

    /// <summary>
    ///     Verifies the review parser defaults to not approved when the approval line is missing.
    /// </summary>
    [Fact]
    public void ParseReviewDecisionDefaultsToNotApprovedWhenApprovalLineIsMissing()
    {
        WorkflowReviewDecision decision = MarkdownArtifactService.ParseReviewDecision("# Review\n## Verdict\nNeeds work\n");

        Assert.False(decision.Approved);
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
}
