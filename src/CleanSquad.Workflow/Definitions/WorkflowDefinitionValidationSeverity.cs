namespace CleanSquad.Workflow.Definitions;

/// <summary>
///     Describes the severity of a workflow-definition validation issue.
/// </summary>
public enum WorkflowDefinitionValidationSeverity
{
    /// <summary>
    ///     The issue makes the workflow definition invalid.
    /// </summary>
    Error,

    /// <summary>
    ///     The issue does not block execution but should be reviewed.
    /// </summary>
    Warning,
}
