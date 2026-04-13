using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CleanSquad.Workflow;
using CleanSquad.Workflow.Decisions;

namespace CleanSquad.Cli.UnitTests.Workflows;

/// <summary>
///     Unit tests for the workflow-specific CLI path.
/// </summary>
public sealed class WorkflowCliTests
{
    /// <summary>
    ///     Verifies the workflow run command delegates to the orchestrator and returns a useful summary.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task BuildOutputAsyncRunsWorkflowCommandAsync()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            string workflowDefinitionPath = Path.Combine(tempDirectoryPath, "workflow.json");
            string requestPath = Path.Combine(tempDirectoryPath, "request.md");
            await File.WriteAllTextAsync(workflowDefinitionPath, "{}", Encoding.UTF8);
            await File.WriteAllTextAsync(requestPath, "# Request\n", System.Text.Encoding.UTF8);
            using StringWriter outputWriter = new();

            FakeWorkflowOrchestrator orchestrator = new(
                new WorkflowRunResult(
                    Path.Combine(tempDirectoryPath, ".workflow-testing", "workflow-runs", "demo"),
                    Path.Combine(tempDirectoryPath, ".workflow-testing", "workflow-runs", "demo", "final.md"),
                    Path.Combine(tempDirectoryPath, ".workflow-testing", "workflow-runs", "demo", "state.md"),
                    WorkflowRunStatus.Approved,
                    true,
                    0,
                    1,
                    Array.Empty<WorkflowDecision>()));

            int exitCode = await CliApplication.InvokeAsync(
                ["workflow", "run", "--definition", workflowDefinitionPath, requestPath, "--entry-point", "review"],
                orchestrator,
                tempDirectoryPath,
                outputWriter);
            string output = outputWriter.ToString().Trim();

            Assert.Equal(0, exitCode);
            Assert.Contains("Workflow finished with reviewer approval.", output, StringComparison.Ordinal);
            Assert.Equal(tempDirectoryPath, orchestrator.LastRequest?.WorkspaceRootPath);
            Assert.Equal(Path.GetFullPath(workflowDefinitionPath), orchestrator.LastRequest?.WorkflowDefinitionPath);
            Assert.Equal(Path.GetFullPath(requestPath), orchestrator.LastRequest?.RequestDocumentPath);
            Assert.Equal("review", orchestrator.LastRequest?.EntryPointId);
        }
        finally
        {
            Directory.Delete(tempDirectoryPath, true);
        }
    }

    /// <summary>
    ///     Verifies the workflow run command accepts inline markdown request text.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task BuildOutputAsyncRunsWorkflowCommandFromInlineRequestTextAsync()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            string workflowDefinitionPath = Path.Combine(tempDirectoryPath, "workflow.json");
            await File.WriteAllTextAsync(workflowDefinitionPath, "{}", Encoding.UTF8);
            using StringWriter outputWriter = new();

            FakeWorkflowOrchestrator orchestrator = new(
                new WorkflowRunResult(
                    Path.Combine(tempDirectoryPath, ".workflow-testing", "workflow-runs", "demo"),
                    Path.Combine(tempDirectoryPath, ".workflow-testing", "workflow-runs", "demo", "final.md"),
                    Path.Combine(tempDirectoryPath, ".workflow-testing", "workflow-runs", "demo", "state.md"),
                    WorkflowRunStatus.Approved,
                    true,
                    0,
                    1,
                    Array.Empty<WorkflowDecision>()));

            const string inlineRequest = "# Inline request\n\nDo the thing.";
            int exitCode = await CliApplication.InvokeAsync(
                ["workflow", "run", "--definition", workflowDefinitionPath, "--request-text", inlineRequest, "--entry-point", "review"],
                orchestrator,
                tempDirectoryPath,
                outputWriter);
            string output = outputWriter.ToString().Trim();

            Assert.Equal(0, exitCode);
            Assert.Contains("Workflow finished with reviewer approval.", output, StringComparison.Ordinal);
            Assert.Equal(tempDirectoryPath, orchestrator.LastRequest?.WorkspaceRootPath);
            Assert.Equal(Path.GetFullPath(workflowDefinitionPath), orchestrator.LastRequest?.WorkflowDefinitionPath);
            Assert.EndsWith("inline-request.md", orchestrator.LastRequest?.RequestDocumentPath, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("# Inline request\n\nDo the thing.\n", orchestrator.LastRequestMarkdown);
            Assert.False(File.Exists(orchestrator.LastRequest?.RequestDocumentPath));
            Assert.Equal("review", orchestrator.LastRequest?.EntryPointId);
        }
        finally
        {
            Directory.Delete(tempDirectoryPath, true);
        }
    }

    /// <summary>
    ///     Verifies the workflow resume command delegates to the orchestrator.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task ResumeCommandDelegatesToTheOrchestratorAsync()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            string runDirectoryPath = Path.Combine(tempDirectoryPath, ".workflow-testing", "workflow-runs", "demo");
            Directory.CreateDirectory(runDirectoryPath);
            await File.WriteAllTextAsync(Path.Combine(runDirectoryPath, "state.json"), "{}", Encoding.UTF8);
            using StringWriter outputWriter = new();

            FakeWorkflowOrchestrator orchestrator = new(
                new WorkflowRunResult(
                    runDirectoryPath,
                    Path.Combine(runDirectoryPath, "final.md"),
                    Path.Combine(runDirectoryPath, "state.md"),
                    WorkflowRunStatus.Stopped,
                    false,
                    1,
                    2,
                    Array.Empty<WorkflowDecision>()));

            int exitCode = await CliApplication.InvokeAsync(
                ["workflow", "resume", runDirectoryPath, "--entry-point", "builder"],
                orchestrator,
                tempDirectoryPath,
                outputWriter);

            Assert.Equal(0, exitCode);
            Assert.Equal(runDirectoryPath, orchestrator.LastRequest?.ResumePath);
            Assert.Equal("builder", orchestrator.LastRequest?.EntryPointId);
        }
        finally
        {
            Directory.Delete(tempDirectoryPath, true);
        }
    }

    /// <summary>
    ///     Verifies paused workflows are reported as intentional waits rather than failures.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task BuildOutputAsyncReportsPausedWorkflowStatusAsync()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            string workflowDefinitionPath = Path.Combine(tempDirectoryPath, "workflow.json");
            string requestPath = Path.Combine(tempDirectoryPath, "request.md");
            await File.WriteAllTextAsync(workflowDefinitionPath, "{}", Encoding.UTF8);
            await File.WriteAllTextAsync(requestPath, "# Request\n", Encoding.UTF8);
            using StringWriter outputWriter = new();

            FakeWorkflowOrchestrator orchestrator = new(
                new WorkflowRunResult(
                    Path.Combine(tempDirectoryPath, ".workflow-testing", "workflow-runs", "demo"),
                    Path.Combine(tempDirectoryPath, ".workflow-testing", "workflow-runs", "demo", "final.md"),
                    Path.Combine(tempDirectoryPath, ".workflow-testing", "workflow-runs", "demo", "state.md"),
                    WorkflowRunStatus.Paused,
                    false,
                    0,
                    0,
                    Array.Empty<WorkflowDecision>()));

            int exitCode = await CliApplication.InvokeAsync(
                ["workflow", "run", "--definition", workflowDefinitionPath, requestPath],
                orchestrator,
                tempDirectoryPath,
                outputWriter);
            string output = outputWriter.ToString().Trim();

            Assert.Equal(0, exitCode);
            Assert.Contains("paused and is waiting to be resumed", output, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(tempDirectoryPath, true);
        }
    }

    /// <summary>
    ///     Verifies the workflow command rejects missing request files.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task BuildOutputAsyncRejectsMissingRequestFileAsync()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            string workflowDefinitionPath = Path.Combine(tempDirectoryPath, "workflow.json");
            await File.WriteAllTextAsync(workflowDefinitionPath, "{}", Encoding.UTF8);
            using StringWriter outputWriter = new();

            int exitCode = await CliApplication.InvokeAsync(
                ["workflow", "run", "--definition", workflowDefinitionPath, "missing.md"],
                currentDirectory: tempDirectoryPath,
                outputWriter: outputWriter);
            string output = outputWriter.ToString().Trim();

            Assert.Equal(1, exitCode);
            Assert.StartsWith("Request markdown file not found:", output, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(tempDirectoryPath, true);
        }
    }

    /// <summary>
    ///     Verifies the workflow command requires markdown input.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task BuildOutputAsyncRejectsNonMarkdownRequestFileAsync()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            string workflowDefinitionPath = Path.Combine(tempDirectoryPath, "workflow.json");
            string requestPath = Path.Combine(tempDirectoryPath, "request.txt");
            await File.WriteAllTextAsync(workflowDefinitionPath, "{}", Encoding.UTF8);
            await File.WriteAllTextAsync(requestPath, "request", System.Text.Encoding.UTF8);
            using StringWriter outputWriter = new();

            int exitCode = await CliApplication.InvokeAsync(
                ["workflow", "run", "--definition", workflowDefinitionPath, requestPath],
                currentDirectory: tempDirectoryPath,
                outputWriter: outputWriter);
            string output = outputWriter.ToString().Trim();

            Assert.Equal(1, exitCode);
            Assert.Equal("The workflow command requires a markdown (.md) request document path.", output);
        }
        finally
        {
            Directory.Delete(tempDirectoryPath, true);
        }
    }

    /// <summary>
    ///     Verifies the workflow run command rejects conflicting file and inline request inputs.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task BuildOutputAsyncRejectsConflictingRequestInputsAsync()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            string workflowDefinitionPath = Path.Combine(tempDirectoryPath, "workflow.json");
            string requestPath = Path.Combine(tempDirectoryPath, "request.md");
            await File.WriteAllTextAsync(workflowDefinitionPath, "{}", Encoding.UTF8);
            await File.WriteAllTextAsync(requestPath, "# Request\n", Encoding.UTF8);
            using StringWriter outputWriter = new();

            int exitCode = await CliApplication.InvokeAsync(
                ["workflow", "run", "--definition", workflowDefinitionPath, requestPath, "--request-text", "# Inline request"],
                currentDirectory: tempDirectoryPath,
                outputWriter: outputWriter);
            string output = outputWriter.ToString().Trim();

            Assert.Equal(1, exitCode);
            Assert.Equal("Specify either a markdown (.md) request document path or --request-text, but not both.", output);
        }
        finally
        {
            Directory.Delete(tempDirectoryPath, true);
        }
    }

    /// <summary>
    ///     Verifies the workflow validate command reports success for a valid definition.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task ValidateCommandReportsSuccessAsync()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            string assetsDirectoryPath = Path.Combine(tempDirectoryPath, "assets");
            Directory.CreateDirectory(assetsDirectoryPath);
            await File.WriteAllTextAsync(Path.Combine(assetsDirectoryPath, "planner.md"), "planner", Encoding.UTF8);
            string workflowDefinitionPath = Path.Combine(tempDirectoryPath, "workflow.json");
            string definitionJson = string.Join(
                Environment.NewLine,
                "{",
                "  \"name\": \"Validation Test\",",
                "  \"defaultEntryPoint\": \"default\",",
                "  \"entryPoints\": [{ \"id\": \"default\", \"nodeId\": \"planner\" }],",
                "  \"nodes\": [",
                "    {",
                "      \"id\": \"planner\",",
                "      \"kind\": \"Stage\",",
                "      \"role\": \"Planner\",",
                "      \"assets\": [{ \"kind\": \"agent\", \"path\": \"assets/planner.md\" }],",
                "      \"next\": \"approved\"",
                "    },",
                "    { \"id\": \"approved\", \"kind\": \"Exit\", \"exitStatus\": \"Approved\" }",
                "  ],",
                "  \"policy\": { \"decisionMode\": \"Rules\", \"maxReviewCycles\": 2, \"maxRebuilds\": 1 }",
                "}");
            await File.WriteAllTextAsync(workflowDefinitionPath, definitionJson, Encoding.UTF8);
            using StringWriter outputWriter = new();

            int exitCode = await CliApplication.InvokeAsync(
                ["workflow", "validate", "--definition", workflowDefinitionPath],
                currentDirectory: tempDirectoryPath,
                outputWriter: outputWriter);
            string output = outputWriter.ToString();

            Assert.Equal(0, exitCode);
            Assert.Contains("Workflow definition is valid.", output, StringComparison.Ordinal);
            Assert.Contains("Nodes: 2", output, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(tempDirectoryPath, true);
        }
    }

    /// <summary>
    ///     Verifies the workflow validate command reports failures for an invalid definition.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task ValidateCommandReportsFailureAsync()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            string workflowDefinitionPath = Path.Combine(tempDirectoryPath, "workflow.json");
            string definitionJson = string.Join(
                Environment.NewLine,
                "{",
                "  \"name\": \"Validation Failure Test\",",
                "  \"defaultEntryPoint\": \"default\",",
                "  \"entryPoints\": [{ \"id\": \"default\", \"nodeId\": \"planner\" }],",
                "  \"nodes\": [",
                "    {",
                "      \"id\": \"planner\",",
                "      \"kind\": \"Stage\",",
                "      \"role\": \"Planner\",",
                "      \"assets\": [{ \"kind\": \"agent\", \"path\": \"assets/missing.md\" }],",
                "      \"next\": \"approved\"",
                "    },",
                "    { \"id\": \"approved\", \"kind\": \"Exit\", \"exitStatus\": \"Approved\" }",
                "  ],",
                "  \"policy\": { \"decisionMode\": \"Rules\", \"maxReviewCycles\": 2, \"maxRebuilds\": 1 }",
                "}");
            await File.WriteAllTextAsync(workflowDefinitionPath, definitionJson, Encoding.UTF8);
            using StringWriter outputWriter = new();

            int exitCode = await CliApplication.InvokeAsync(
                ["workflow", "validate", "--definition", workflowDefinitionPath],
                currentDirectory: tempDirectoryPath,
                outputWriter: outputWriter);
            string output = outputWriter.ToString();

            Assert.Equal(1, exitCode);
            Assert.Contains("Workflow definition is invalid.", output, StringComparison.Ordinal);
            Assert.Contains("Errors:", output, StringComparison.Ordinal);
            Assert.Contains("could not be found", output, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDirectoryPath, true);
        }
    }

    /// <summary>
    ///     Verifies the workflow diagram command writes a markdown file containing Mermaid markup.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task DiagramCommandWritesMarkdownFileAsync()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            string assetsDirectoryPath = Path.Combine(tempDirectoryPath, "assets");
            Directory.CreateDirectory(assetsDirectoryPath);
            await File.WriteAllTextAsync(Path.Combine(assetsDirectoryPath, "planner.md"), "planner", Encoding.UTF8);
            string workflowDefinitionPath = Path.Combine(tempDirectoryPath, "workflow.json");
            string outputPath = Path.Combine(tempDirectoryPath, "docs", "workflow-diagram.md");
            string definitionJson = string.Join(
                Environment.NewLine,
                "{",
                "  \"name\": \"Diagram Test\",",
                "  \"defaultEntryPoint\": \"default\",",
                "  \"entryPoints\": [{ \"id\": \"default\", \"nodeId\": \"planner\" }],",
                "  \"nodes\": [",
                "    {",
                "      \"id\": \"planner\",",
                "      \"kind\": \"Stage\",",
                "      \"displayName\": \"Planner\",",
                "      \"assets\": [{ \"kind\": \"agent\", \"path\": \"assets/planner.md\" }],",
                "      \"next\": \"approved\"",
                "    },",
                "    { \"id\": \"approved\", \"kind\": \"Exit\", \"displayName\": \"Approved\", \"exitStatus\": \"Approved\" }",
                "  ],",
                "  \"policy\": { \"decisionMode\": \"Rules\", \"maxReviewCycles\": 2, \"maxRebuilds\": 1 }",
                "}");
            await File.WriteAllTextAsync(workflowDefinitionPath, definitionJson, Encoding.UTF8);
            using StringWriter outputWriter = new();

            int exitCode = await CliApplication.InvokeAsync(
                ["workflow", "diagram", "--definition", workflowDefinitionPath, "--output", outputPath],
                currentDirectory: tempDirectoryPath,
                outputWriter: outputWriter);
            string output = outputWriter.ToString().Trim();
            string markdown = await File.ReadAllTextAsync(outputPath, Encoding.UTF8);

            Assert.Equal(0, exitCode);
            Assert.Equal($"Workflow diagram markdown written to: {outputPath}", output);
            Assert.Contains("# Workflow Diagram", markdown, StringComparison.Ordinal);
            Assert.Contains("```mermaid", markdown, StringComparison.Ordinal);
            Assert.Contains("flowchart TD", markdown, StringComparison.Ordinal);
            Assert.Contains("Entry: default", markdown, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(tempDirectoryPath, true);
        }
    }

    /// <summary>
    ///     Verifies the workflow diagram command uses the default markdown output path when none is provided.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task DiagramCommandUsesDefaultOutputPathAsync()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            string assetsDirectoryPath = Path.Combine(tempDirectoryPath, "assets");
            Directory.CreateDirectory(assetsDirectoryPath);
            await File.WriteAllTextAsync(Path.Combine(assetsDirectoryPath, "planner.md"), "planner", Encoding.UTF8);
            string workflowDefinitionPath = Path.Combine(tempDirectoryPath, "workflow.graph.json");
            string definitionJson = string.Join(
                Environment.NewLine,
                "{",
                "  \"name\": \"Default Diagram Path Test\",",
                "  \"defaultEntryPoint\": \"default\",",
                "  \"entryPoints\": [{ \"id\": \"default\", \"nodeId\": \"planner\" }],",
                "  \"nodes\": [",
                "    {",
                "      \"id\": \"planner\",",
                "      \"kind\": \"Stage\",",
                "      \"assets\": [{ \"kind\": \"agent\", \"path\": \"assets/planner.md\" }],",
                "      \"next\": \"approved\"",
                "    },",
                "    { \"id\": \"approved\", \"kind\": \"Exit\", \"exitStatus\": \"Approved\" }",
                "  ],",
                "  \"policy\": { \"decisionMode\": \"Rules\", \"maxReviewCycles\": 2, \"maxRebuilds\": 1 }",
                "}");
            await File.WriteAllTextAsync(workflowDefinitionPath, definitionJson, Encoding.UTF8);
            using StringWriter outputWriter = new();
            string expectedOutputPath = Path.Combine(tempDirectoryPath, "workflow.graph.diagram.md");

            int exitCode = await CliApplication.InvokeAsync(
                ["workflow", "diagram", "--definition", workflowDefinitionPath],
                currentDirectory: tempDirectoryPath,
                outputWriter: outputWriter);

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(expectedOutputPath));
            Assert.Contains(expectedOutputPath, outputWriter.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(tempDirectoryPath, true);
        }
    }

    private static string CreateTempDirectory()
    {
        string tempDirectoryPath = Path.Combine(Path.GetTempPath(), $"clean-squad-cli-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectoryPath);
        return tempDirectoryPath;
    }

    private sealed class FakeWorkflowOrchestrator : IWorkflowOrchestrator
    {
        private readonly WorkflowRunResult result;

        public FakeWorkflowOrchestrator(WorkflowRunResult result)
        {
            this.result = result;
        }

        public WorkflowExecutionRequest? LastRequest { get; private set; }

        public string? LastRequestMarkdown { get; private set; }

        public async Task<WorkflowRunResult> ExecuteAsync(WorkflowExecutionRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.LastRequest = request;
            if (!string.IsNullOrWhiteSpace(request.RequestDocumentPath) && File.Exists(request.RequestDocumentPath))
            {
                this.LastRequestMarkdown = await File.ReadAllTextAsync(request.RequestDocumentPath, Encoding.UTF8, cancellationToken);
            }

            return this.result;
        }

        public Task<WorkflowRunResult> ExecuteAsync(
            string workspaceRootPath,
            string workflowDefinitionPath,
            string requestDocumentPath,
            CancellationToken cancellationToken = default)
        {
            return this.ExecuteAsync(new WorkflowExecutionRequest(workspaceRootPath, workflowDefinitionPath, requestDocumentPath), cancellationToken);
        }
    }
}
