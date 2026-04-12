namespace CleanSquad.Workflow.Definitions;

/// <summary>
///     Represents one workflow-definition validation issue.
/// </summary>
/// <param name="Severity">The issue severity.</param>
/// <param name="Message">The issue message.</param>
public sealed record WorkflowDefinitionValidationIssue(WorkflowDefinitionValidationSeverity Severity, string Message);
