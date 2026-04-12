namespace CleanSquad.Workflow.Definitions;

/// <summary>
///     Describes an external file asset used by a workflow stage.
/// </summary>
/// <param name="Kind">The logical asset kind, such as agent, skill, or instruction.</param>
/// <param name="Path">The path to the asset file, relative to the workflow definition or absolute.</param>
/// <param name="Hash">The optional expected SHA-256 hash for the asset file.</param>
public sealed record WorkflowAssetReference(string Kind, string Path, string? Hash = null);
