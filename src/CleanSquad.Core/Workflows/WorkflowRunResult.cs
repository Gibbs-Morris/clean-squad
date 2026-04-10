namespace CleanSquad.Core.Workflows;

/// <summary>
///     Captures the outcome of a workflow execution.
/// </summary>
/// <param name="RunDirectoryPath">The folder containing the generated workflow artifacts.</param>
/// <param name="FinalArtifactPath">The path to the final workflow markdown artifact.</param>
/// <param name="StateArtifactPath">The path to the workflow state markdown artifact.</param>
/// <param name="Status">The final workflow status.</param>
/// <param name="ReviewApproved">Whether the reviewer explicitly approved the first build.</param>
/// <param name="RebuildPerformed">Whether the workflow ran a rebuild after review feedback.</param>
public sealed record WorkflowRunResult(
    string RunDirectoryPath,
    string FinalArtifactPath,
    string StateArtifactPath,
    WorkflowRunStatus Status,
    bool ReviewApproved,
    bool RebuildPerformed)
{
    /// <summary>
    ///     Gets a concise user-facing summary of the outcome.
    /// </summary>
    public string StatusLabel => this.Status switch
    {
        WorkflowRunStatus.Approved => "finished with reviewer approval",
        WorkflowRunStatus.RebuiltAfterReview => "finished after one rebuild from review feedback",
        _ => "finished",
    };
}
