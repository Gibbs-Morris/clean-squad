using System;
using CleanSquad.Workflow;
using CleanSquad.Workflow.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CleanSquad.Core;

/// <summary>
///     Creates the default workflow orchestrator used by the application.
/// </summary>
public static class WorkflowOrchestratorFactory
{
    /// <summary>
    ///     Creates the default workflow orchestrator for a workspace.
    /// </summary>
    /// <param name="workspaceRootPath">The workspace root path.</param>
    /// <param name="timeProvider">The optional time provider for artifact creation.</param>
    /// <param name="loggerFactory">The optional logger factory.</param>
    /// <param name="storageOptions">The optional workflow storage options.</param>
    /// <returns>The configured workflow orchestrator.</returns>
    public static IWorkflowOrchestrator Create(
        string workspaceRootPath,
        TimeProvider? timeProvider = null,
        ILoggerFactory? loggerFactory = null,
        IOptions<WorkflowStorageOptions>? storageOptions = null)
    {
        return Workflows.CopilotWorkflowOrchestratorFactory.Create(workspaceRootPath, timeProvider, loggerFactory, storageOptions);
    }
}
