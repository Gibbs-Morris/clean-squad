using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CleanSquad.Core.Workflows;

/// <summary>
///     Coordinates the end-to-end planner, builder, reviewer, and rebuild workflow.
/// </summary>
public sealed class WorkflowOrchestrator : IWorkflowOrchestrator
{
    private readonly MarkdownArtifactService markdownArtifactService;
    private readonly IWorkflowAgentRunner workflowAgentRunner;

    /// <summary>
    ///     Initializes a new instance of the <see cref="WorkflowOrchestrator" /> class.
    /// </summary>
    /// <param name="markdownArtifactService">The markdown artifact service.</param>
    /// <param name="workflowAgentRunner">The workflow agent runner.</param>
    public WorkflowOrchestrator(
        MarkdownArtifactService markdownArtifactService,
        IWorkflowAgentRunner workflowAgentRunner)
    {
        this.markdownArtifactService = markdownArtifactService ?? throw new ArgumentNullException(nameof(markdownArtifactService));
        this.workflowAgentRunner = workflowAgentRunner ?? throw new ArgumentNullException(nameof(workflowAgentRunner));
    }

    /// <inheritdoc />
    public async Task<WorkflowRunResult> ExecuteAsync(
        string repositoryRootPath,
        string requestDocumentPath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        WorkflowArtifacts artifacts = this.markdownArtifactService.CreateRunArtifacts(repositoryRootPath, requestDocumentPath);

        string planMarkdown = await this.workflowAgentRunner.RunAsync(
            WorkflowStage.Planner,
            WorkflowPromptFactory.CreatePlannerPrompt(artifacts),
            [artifacts.RequestMarkdownPath],
            cancellationToken);
        MarkdownArtifactService.WriteMarkdown(artifacts.PlanMarkdownPath, planMarkdown);

        string buildMarkdown = await this.workflowAgentRunner.RunAsync(
            WorkflowStage.Builder,
            WorkflowPromptFactory.CreateBuilderPrompt(artifacts),
            [artifacts.RequestMarkdownPath, artifacts.PlanMarkdownPath],
            cancellationToken);
        MarkdownArtifactService.WriteMarkdown(artifacts.BuildMarkdownPath, buildMarkdown);

        string reviewMarkdown = await this.workflowAgentRunner.RunAsync(
            WorkflowStage.Reviewer,
            WorkflowPromptFactory.CreateReviewerPrompt(artifacts),
            [artifacts.RequestMarkdownPath, artifacts.PlanMarkdownPath, artifacts.BuildMarkdownPath],
            cancellationToken);
        MarkdownArtifactService.WriteMarkdown(artifacts.ReviewMarkdownPath, reviewMarkdown);

        WorkflowReviewDecision reviewDecision = MarkdownArtifactService.ParseReviewDecision(reviewMarkdown);
        string finalBuildMarkdown = buildMarkdown;
        WorkflowRunStatus status = WorkflowRunStatus.Approved;
        bool rebuildPerformed = false;

        if (!reviewDecision.Approved)
        {
            rebuildPerformed = true;
            finalBuildMarkdown = await this.workflowAgentRunner.RunAsync(
                WorkflowStage.Rebuilder,
                WorkflowPromptFactory.CreateRebuilderPrompt(artifacts),
                [artifacts.RequestMarkdownPath, artifacts.PlanMarkdownPath, artifacts.BuildMarkdownPath, artifacts.ReviewMarkdownPath],
                cancellationToken);
            MarkdownArtifactService.WriteMarkdown(artifacts.RebuildMarkdownPath, finalBuildMarkdown);
            status = WorkflowRunStatus.RebuiltAfterReview;
        }

        string finalMarkdown = BuildFinalMarkdown(artifacts, status, finalBuildMarkdown, reviewMarkdown, rebuildPerformed);
        MarkdownArtifactService.WriteMarkdown(artifacts.FinalMarkdownPath, finalMarkdown);

        WorkflowRunResult result = new(
            artifacts.RunDirectoryPath,
            artifacts.FinalMarkdownPath,
            artifacts.StateMarkdownPath,
            status,
            reviewDecision.Approved,
            rebuildPerformed);
        MarkdownArtifactService.WriteState(artifacts, result);

        return result;
    }

    private static string BuildFinalMarkdown(
        WorkflowArtifacts artifacts,
        WorkflowRunStatus status,
        string finalBuildMarkdown,
        string reviewMarkdown,
        bool rebuildPerformed)
    {
        string finalArtifactName = rebuildPerformed
            ? Path.GetFileName(artifacts.RebuildMarkdownPath)
            : Path.GetFileName(artifacts.BuildMarkdownPath);

        return $"""
# Final Workflow Output
Status: {status}
ReviewApproved: {ToYesNo(status == WorkflowRunStatus.Approved)}
RebuildPerformed: {ToYesNo(rebuildPerformed)}

## Run Files
- request: {Path.GetFileName(artifacts.RequestMarkdownPath)}
- plan: {Path.GetFileName(artifacts.PlanMarkdownPath)}
- build: {Path.GetFileName(artifacts.BuildMarkdownPath)}
- review: {Path.GetFileName(artifacts.ReviewMarkdownPath)}
- final artifact: {finalArtifactName}

## Final Artifact Content
{finalBuildMarkdown.Trim()}

## Review Content
{reviewMarkdown.Trim()}
""";
    }

    private static string ToYesNo(bool value)
    {
        return value ? "yes" : "no";
    }
}
