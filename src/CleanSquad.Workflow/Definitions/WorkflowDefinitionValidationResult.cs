using System;
using System.Collections.Generic;
using System.Linq;

namespace CleanSquad.Workflow.Definitions;

/// <summary>
///     Represents the result of validating a workflow definition.
/// </summary>
public sealed class WorkflowDefinitionValidationResult
{
    private readonly string[] errors;
    private readonly string[] warnings;

    /// <summary>
    ///     Initializes a new instance of the <see cref="WorkflowDefinitionValidationResult" /> class.
    /// </summary>
    /// <param name="workflowDefinitionPath">The workflow definition path.</param>
    /// <param name="definition">The normalized workflow definition when deserialization succeeded.</param>
    /// <param name="issues">The validation issues.</param>
    public WorkflowDefinitionValidationResult(
        string workflowDefinitionPath,
        WorkflowDefinition? definition,
        IReadOnlyList<WorkflowDefinitionValidationIssue> issues)
    {
        this.WorkflowDefinitionPath = workflowDefinitionPath ?? throw new ArgumentNullException(nameof(workflowDefinitionPath));
        this.Definition = definition;
        this.Issues = issues ?? throw new ArgumentNullException(nameof(issues));
        this.errors = this.Issues
            .Where(issue => issue.Severity == WorkflowDefinitionValidationSeverity.Error)
            .Select(issue => issue.Message)
            .ToArray();
        this.warnings = this.Issues
            .Where(issue => issue.Severity == WorkflowDefinitionValidationSeverity.Warning)
            .Select(issue => issue.Message)
            .ToArray();
    }

    /// <summary>
    ///     Gets the workflow definition path that was validated.
    /// </summary>
    public string WorkflowDefinitionPath { get; }

    /// <summary>
    ///     Gets the normalized workflow definition when parsing succeeded.
    /// </summary>
    public WorkflowDefinition? Definition { get; }

    /// <summary>
    ///     Gets the validation issues.
    /// </summary>
    public IReadOnlyList<WorkflowDefinitionValidationIssue> Issues { get; }

    /// <summary>
    ///     Gets a value indicating whether the workflow definition is valid.
    /// </summary>
    public bool IsValid => this.errors.Length == 0;

    /// <summary>
    ///     Gets the validation errors.
    /// </summary>
    public IReadOnlyList<string> Errors => this.errors;

    /// <summary>
    ///     Gets the validation warnings.
    /// </summary>
    public IReadOnlyList<string> Warnings => this.warnings;

    /// <summary>
    ///     Gets the number of graph nodes in the normalized definition.
    /// </summary>
    public int NodeCount => this.Definition?.Nodes.Count ?? 0;

    /// <summary>
    ///     Gets the number of entry points in the normalized definition.
    /// </summary>
    public int EntryPointCount => this.Definition?.EntryPoints.Count ?? 0;

    /// <summary>
    ///     Gets the number of referenced workflow assets in the normalized definition.
    /// </summary>
    public int AssetCount => this.Definition is null
        ? 0
        : this.Definition.SharedAssets.Count + this.Definition.Nodes.Sum(node => node.Assets.Count);
}
