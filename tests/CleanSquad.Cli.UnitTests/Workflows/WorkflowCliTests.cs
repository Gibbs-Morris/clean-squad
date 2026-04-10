using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CleanSquad.Core.Workflows;

namespace CleanSquad.Cli.UnitTests.Workflows;

/// <summary>
///     Unit tests for the workflow-specific CLI path.
/// </summary>
public sealed class WorkflowCliTests
{
    /// <summary>
    ///     Verifies the workflow command delegates to the orchestrator and returns a useful summary.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task BuildOutputAsyncRunsWorkflowCommandAsync()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempDirectoryPath, "CleanSquad.slnx"), "<Solution />", System.Text.Encoding.UTF8);
            string requestPath = Path.Combine(tempDirectoryPath, "request.md");
            await File.WriteAllTextAsync(requestPath, "# Request\n", System.Text.Encoding.UTF8);

            FakeWorkflowOrchestrator orchestrator = new(
                new WorkflowRunResult(
                    Path.Combine(tempDirectoryPath, "workflow-runs", "demo"),
                    Path.Combine(tempDirectoryPath, "workflow-runs", "demo", "final.md"),
                    Path.Combine(tempDirectoryPath, "workflow-runs", "demo", "state.md"),
                    WorkflowRunStatus.Approved,
                    true,
                    false));

            string output = await CliApplication.BuildOutputAsync(["workflow", requestPath], orchestrator, tempDirectoryPath);

            Assert.Contains("Workflow finished with reviewer approval.", output, StringComparison.Ordinal);
            Assert.Equal(tempDirectoryPath, orchestrator.RepositoryRootPath);
            Assert.Equal(Path.GetFullPath(requestPath), orchestrator.RequestDocumentPath);
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
            await File.WriteAllTextAsync(Path.Combine(tempDirectoryPath, "CleanSquad.slnx"), "<Solution />", System.Text.Encoding.UTF8);

            string output = await CliApplication.BuildOutputAsync(["workflow", "missing.md"], currentDirectory: tempDirectoryPath);

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
            await File.WriteAllTextAsync(Path.Combine(tempDirectoryPath, "CleanSquad.slnx"), "<Solution />", System.Text.Encoding.UTF8);
            string requestPath = Path.Combine(tempDirectoryPath, "request.txt");
            await File.WriteAllTextAsync(requestPath, "request", System.Text.Encoding.UTF8);

            string output = await CliApplication.BuildOutputAsync(["workflow", requestPath], currentDirectory: tempDirectoryPath);

            Assert.Equal("The workflow command requires a markdown (.md) request document path.", output);
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

        public string? RepositoryRootPath { get; private set; }

        public string? RequestDocumentPath { get; private set; }

        public Task<WorkflowRunResult> ExecuteAsync(
            string repositoryRootPath,
            string requestDocumentPath,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.RepositoryRootPath = repositoryRootPath;
            this.RequestDocumentPath = requestDocumentPath;
            return Task.FromResult(this.result);
        }
    }
}
