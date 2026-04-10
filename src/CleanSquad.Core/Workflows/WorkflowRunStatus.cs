namespace CleanSquad.Core.Workflows;

/// <summary>
///     Describes the final state of a workflow run.
/// </summary>
public enum WorkflowRunStatus
{
    /// <summary>
    ///     The reviewer approved the first build.
    /// </summary>
    Approved,

    /// <summary>
    ///     The builder produced one revised artifact after review feedback.
    /// </summary>
    RebuiltAfterReview,
}
