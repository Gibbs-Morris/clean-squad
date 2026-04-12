using System;
using System.IO;
using System.Threading.Tasks;
using CleanSquad.Workflow.Definitions;
using CleanSquad.Workflow.Prompting;

namespace CleanSquad.Workflow.UnitTests.Prompting;

/// <summary>
///     Unit tests for <see cref="WorkflowAssetLoader" />.
/// </summary>
public sealed class WorkflowAssetLoaderTests
{
    /// <summary>
    ///     Verifies the loader emits one markdown block per asset with trimmed file content.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task LoadMarkdownBlocksAsyncFormatsEachAssetAsMarkdownAsync()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            string instructionPath = Path.Combine(tempDirectoryPath, "instruction.md");
            string agentPath = Path.Combine(tempDirectoryPath, "agent.md");
            await File.WriteAllTextAsync(instructionPath, "instruction content\n\n", System.Text.Encoding.UTF8);
            await File.WriteAllTextAsync(agentPath, "agent content\r\n", System.Text.Encoding.UTF8);

            string markdown = await WorkflowAssetLoader.LoadMarkdownBlocksAsync(
                [
                    new WorkflowAssetReference("instruction", instructionPath),
                    new WorkflowAssetReference("agent", agentPath),
                ]);

            Assert.Contains("### instruction: instruction.md", markdown, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("instruction content", markdown, StringComparison.Ordinal);
            Assert.Contains("### agent: agent.md", markdown, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("agent content", markdown, StringComparison.Ordinal);
            Assert.DoesNotContain("agent content\r", markdown, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(tempDirectoryPath, true);
        }
    }

    private static string CreateTempDirectory()
    {
        string tempDirectoryPath = Path.Combine(Path.GetTempPath(), $"clean-squad-assets-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectoryPath);
        return tempDirectoryPath;
    }
}
