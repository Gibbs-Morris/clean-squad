namespace CleanSquad.Core.Workflows;

/// <summary>
///     Identifies the role currently acting within the workflow.
/// </summary>
public enum WorkflowStage
{
    /// <summary>
    ///     The planning agent.
    /// </summary>
    Planner,

    /// <summary>
    ///     The building agent.
    /// </summary>
    Builder,

    /// <summary>
    ///     The reviewing agent.
    /// </summary>
    Reviewer,

    /// <summary>
    ///     The builder retry after review feedback.
    /// </summary>
    Rebuilder,
}
