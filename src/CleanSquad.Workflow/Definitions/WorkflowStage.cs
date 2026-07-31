namespace CleanSquad.Workflow.Definitions;

/// <summary>
///     Identifies the legacy stage currently acting within the workflow.
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
    ///     The decision-making agent.
    /// </summary>
    Decision,

    /// <summary>
    ///     The builder retry after review feedback.
    /// </summary>
    Rebuilder,
}
