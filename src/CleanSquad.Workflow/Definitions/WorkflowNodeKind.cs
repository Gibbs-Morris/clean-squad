namespace CleanSquad.Workflow.Definitions;

/// <summary>
///     Describes the behavior of a workflow node in the execution graph.
/// </summary>
public enum WorkflowNodeKind
{
    /// <summary>
    ///     Executes an agent-backed stage and produces markdown output.
    /// </summary>
    Stage,

    /// <summary>
    ///     Resolves one branch from a fixed set of choices.
    /// </summary>
    Decision,

    /// <summary>
    ///     Fans execution out into multiple parallel branches.
    /// </summary>
    Fork,

    /// <summary>
    ///     Waits for all branches from a fork to arrive before continuing.
    /// </summary>
    Join,

    /// <summary>
    ///     Terminates the workflow with a final status.
    /// </summary>
    Exit,
}
