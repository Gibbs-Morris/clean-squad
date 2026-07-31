namespace CleanSquad.Workflow.Definitions;

/// <summary>
///     Identifies how post-review decisions are resolved.
/// </summary>
public enum WorkflowDecisionMode
{
    /// <summary>
    ///     Use deterministic rules derived from the reviewer output.
    /// </summary>
    Rules,

    /// <summary>
    ///     Use a configured decision agent stage.
    /// </summary>
    Agent,
}
