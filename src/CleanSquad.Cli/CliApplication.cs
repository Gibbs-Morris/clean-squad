using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CleanSquad.Core;
using CleanSquad.Core.Workflows;

namespace CleanSquad.Cli;

/// <summary>
///     Coordinates the CleanSquad command-line output.
/// </summary>
internal static class CliApplication
{
    /// <summary>
    ///     Builds the output text for the provided command-line arguments.
    /// </summary>
    /// <param name="args">Optional command-line arguments.</param>
    /// <returns>The message that should be printed by the CLI.</returns>
    internal static string BuildOutput(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        string? squadName = args.Length > 0 ? args[0] : null;
        return CleanupChecklistService.CreateSummary(squadName);
    }

    /// <summary>
    ///     Builds the output text for the provided command-line arguments, including workflow execution.
    /// </summary>
    /// <param name="args">Optional command-line arguments.</param>
    /// <param name="workflowOrchestrator">Optional workflow orchestrator for testing.</param>
    /// <param name="currentDirectory">Optional current directory override for testing.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>The message that should be printed by the CLI.</returns>
    internal static async Task<string> BuildOutputAsync(
        string[] args,
        IWorkflowOrchestrator? workflowOrchestrator = null,
        string? currentDirectory = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (!IsWorkflowCommand(args))
        {
            return BuildOutput(args);
        }

        if (args.Length < 2 || string.IsNullOrWhiteSpace(args[1]))
        {
            return "Usage: workflow <path-to-request.md>";
        }

        string workingDirectory = currentDirectory ?? Directory.GetCurrentDirectory();
        string requestDocumentPath = Path.GetFullPath(args[1], workingDirectory);
        if (!File.Exists(requestDocumentPath))
        {
            return $"Request markdown file not found: {requestDocumentPath}";
        }

        if (!string.Equals(Path.GetExtension(requestDocumentPath), ".md", StringComparison.OrdinalIgnoreCase))
        {
            return "The workflow command requires a markdown (.md) request document path.";
        }

        string workspaceRootPath = WorkspaceRootLocator.FindWorkspaceRoot(workingDirectory);
        IWorkflowOrchestrator orchestrator = workflowOrchestrator ?? CreateWorkflowOrchestrator(workspaceRootPath);
        try
        {
            WorkflowRunResult result = await orchestrator.ExecuteAsync(workspaceRootPath, requestDocumentPath, cancellationToken);

            return $"Workflow {result.StatusLabel}. Run folder: {result.RunDirectoryPath}. Final markdown: {result.FinalArtifactPath}";
        }
        catch (IOException ioException)
        {
            return $"Workflow failed: {ioException.Message}";
        }
        catch (InvalidOperationException invalidOperationException)
        {
            return $"Workflow failed: {invalidOperationException.Message}";
        }
    }

    private static WorkflowOrchestrator CreateWorkflowOrchestrator(string workspaceRootPath)
    {
        return new WorkflowOrchestrator(
            new MarkdownArtifactService(TimeProvider.System),
            new CopilotWorkflowAgentRunner(workspaceRootPath));
    }

    private static bool IsWorkflowCommand(string[] args)
    {
        return args.Length > 0 && string.Equals(args[0], "workflow", StringComparison.OrdinalIgnoreCase);
    }
}
