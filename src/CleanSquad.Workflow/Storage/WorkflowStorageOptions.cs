using System;
using System.IO;

namespace CleanSquad.Workflow.Storage;

/// <summary>
///     Configures durable workflow storage locations relative to the workspace root.
/// </summary>
public sealed class WorkflowStorageOptions
{
    /// <summary>
    ///     Gets or sets the directory name used to hold workflow runtime data.
    /// </summary>
    public string WorkflowTestingDirectoryName { get; set; } = ".workflow-testing";

    /// <summary>
    ///     Gets or sets the directory name used to store persisted workflow runs.
    /// </summary>
    public string WorkflowRunsDirectoryName { get; set; } = "workflow-runs";

    /// <summary>
    ///     Gets or sets the directory name reserved for durable local knowledge.
    /// </summary>
    public string KnowledgeDirectoryName { get; set; } = ".knowledge";

    /// <summary>
    ///     Resolves the workflow run storage root for a workspace.
    /// </summary>
    /// <param name="workspaceRootPath">The workspace root path.</param>
    /// <returns>The absolute run storage root.</returns>
    public string GetWorkflowRunsRootPath(string workspaceRootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRootPath);
        ValidateDirectoryName(this.WorkflowTestingDirectoryName, nameof(this.WorkflowTestingDirectoryName));
        ValidateDirectoryName(this.WorkflowRunsDirectoryName, nameof(this.WorkflowRunsDirectoryName));

        return Path.Combine(
            Path.GetFullPath(workspaceRootPath),
            this.WorkflowTestingDirectoryName,
            this.WorkflowRunsDirectoryName);
    }

    /// <summary>
    ///     Resolves the durable local knowledge root for a workspace.
    /// </summary>
    /// <param name="workspaceRootPath">The workspace root path.</param>
    /// <returns>The absolute knowledge root path.</returns>
    public string GetKnowledgeRootPath(string workspaceRootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRootPath);
        ValidateDirectoryName(this.KnowledgeDirectoryName, nameof(this.KnowledgeDirectoryName));
        return Path.Combine(Path.GetFullPath(workspaceRootPath), this.KnowledgeDirectoryName);
    }

    private static void ValidateDirectoryName(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"The workflow storage option '{parameterName}' must not be empty.");
        }
    }
}
