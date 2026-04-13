using System.Collections.Generic;

namespace CleanSquad.Workflow.Definitions;

/// <summary>
///     Represents a JSON-defined workflow configuration.
/// </summary>
public sealed class WorkflowDefinition
{
    /// <summary>
    ///     Gets or sets the schema version.
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    ///     Gets or sets the workflow name.
    /// </summary>
    public string Name { get; set; } = "Default Workflow";

    /// <summary>
    ///     Gets or sets the optional workflow package metadata used for branding and distribution.
    /// </summary>
    public WorkflowPackageDefinition Package { get; set; } = new();

    /// <summary>
    ///     Gets or sets the default entry point identifier.
    /// </summary>
    public string? DefaultEntryPoint { get; set; }

    /// <summary>
    ///     Gets or sets the named workflow entry points.
    /// </summary>
    public IReadOnlyList<WorkflowEntryPointDefinition> EntryPoints { get; set; } = [];

    /// <summary>
    ///     Gets or sets the graph nodes used by the workflow runtime.
    /// </summary>
    public IReadOnlyList<WorkflowNodeDefinition> Nodes { get; set; } = [];

    /// <summary>
    ///     Gets or sets the shared assets applied to every stage.
    /// </summary>
    public IReadOnlyList<WorkflowAssetReference> SharedAssets { get; set; } = [];

    /// <summary>
    ///     Gets or sets the planner stage definition.
    /// </summary>
    public WorkflowStageDefinition? Planner { get; set; }

    /// <summary>
    ///     Gets or sets the builder stage definition.
    /// </summary>
    public WorkflowStageDefinition? Builder { get; set; }

    /// <summary>
    ///     Gets or sets the reviewer stage definition.
    /// </summary>
    public WorkflowStageDefinition? Reviewer { get; set; }

    /// <summary>
    ///     Gets or sets the optional decision stage definition.
    /// </summary>
    public WorkflowStageDefinition? Decision { get; set; }

    /// <summary>
    ///     Gets or sets the rebuilder stage definition.
    /// </summary>
    public WorkflowStageDefinition? Rebuilder { get; set; }

    /// <summary>
    ///     Gets or sets the execution policy.
    /// </summary>
    public WorkflowPolicyDefinition Policy { get; set; } = new();
}
