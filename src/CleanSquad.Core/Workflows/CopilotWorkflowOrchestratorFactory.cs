using System;
using System.IO;
using CleanSquad.Workflow;
using CleanSquad.Workflow.Decisions;
using CleanSquad.Workflow.Orchestration;
using CleanSquad.Workflow.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CleanSquad.Core.Workflows;

/// <summary>
///     Creates the default Copilot-backed workflow orchestrator used by the CLI.
/// </summary>
public static class CopilotWorkflowOrchestratorFactory
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
        ILoggerFactory effectiveLoggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        IOptions<WorkflowStorageOptions> effectiveStorageOptions = storageOptions ?? Options.Create(new WorkflowStorageOptions());
        MarkdownArtifactService artifactService = new(
            effectiveStorageOptions,
            effectiveLoggerFactory.CreateLogger<MarkdownArtifactService>(),
            timeProvider);
        CopilotWorkflowAgentRunner workflowAgentRunner = new(
            Options.Create(new CopilotWorkflowAgentRunnerOptions
            {
                WorkspaceRootPath = workspaceRootPath,
                KnowledgeRootPath = effectiveStorageOptions.Value.GetKnowledgeRootPath(Path.GetFullPath(workspaceRootPath)),
            }),
            effectiveLoggerFactory.CreateLogger<CopilotWorkflowAgentRunner>());
        WorkflowDecisionResolver decisionResolver = new(
            workflowAgentRunner,
            effectiveLoggerFactory.CreateLogger<WorkflowDecisionResolver>());

        return new WorkflowOrchestrator(
            artifactService,
            workflowAgentRunner,
            decisionResolver,
            effectiveLoggerFactory.CreateLogger<WorkflowOrchestrator>(),
            timeProvider);
    }
}
