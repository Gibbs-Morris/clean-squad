using System;
using System.IO;
using System.Threading.Tasks;
using CleanSquad.Workflow.Definitions;
using CleanSquad.Workflow.Prompting;

namespace CleanSquad.Workflow.UnitTests.Prompting;

/// <summary>
///     Unit tests for <see cref="WorkflowPromptComposer" />.
/// </summary>
public sealed class WorkflowPromptComposerTests
{
    /// <summary>
    ///     Verifies the composer includes shared and stage assets.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task CreatePromptAsyncIncludesConfiguredAssetsAsync()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            string sharedAssetPath = Path.Combine(tempDirectoryPath, "shared.md");
            string plannerAssetPath = Path.Combine(tempDirectoryPath, "planner.md");
            await File.WriteAllTextAsync(sharedAssetPath, "shared", System.Text.Encoding.UTF8);
            await File.WriteAllTextAsync(plannerAssetPath, "planner", System.Text.Encoding.UTF8);
            WorkflowDefinition definition = new()
            {
                Name = "Prompt Test",
                SharedAssets = [new WorkflowAssetReference("instruction", sharedAssetPath)],
                Planner = new WorkflowStageDefinition
                {
                    DisplayName = "Planner",
                    Assets = [new WorkflowAssetReference("agent", plannerAssetPath)],
                },
                Builder = new WorkflowStageDefinition(),
                Reviewer = new WorkflowStageDefinition(),
                Rebuilder = new WorkflowStageDefinition(),
            };

            string prompt = await WorkflowPromptComposer.ComposeAsync(definition, WorkflowStage.Planner, ["request.md"]);

            Assert.Contains("instruction: shared.md", prompt, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("agent: planner.md", prompt, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("- request.md", prompt, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(tempDirectoryPath, true);
        }
    }

    /// <summary>
    ///     Verifies the composer includes explicit agent, inputs, outputs, models, and custom message.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task CreatePromptAsyncIncludesStageConfigurationMetadataAsync()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            string sharedAssetPath = Path.Combine(tempDirectoryPath, "shared.md");
            string plannerAssetPath = Path.Combine(tempDirectoryPath, "planner.md");
            await File.WriteAllTextAsync(sharedAssetPath, "shared", System.Text.Encoding.UTF8);
            await File.WriteAllTextAsync(plannerAssetPath, "planner", System.Text.Encoding.UTF8);
            WorkflowDefinition definition = new()
            {
                Name = "Prompt Config Test",
                SharedAssets = [new WorkflowAssetReference("instruction", sharedAssetPath)],
                Nodes =
                [
                    new WorkflowNodeDefinition
                    {
                        Id = "planner",
                        Kind = WorkflowNodeKind.Stage,
                        DisplayName = "Planner",
                        Role = "Planning",
                        Agent = "planner-agent",
                        Models = ["model-plan-fast"],
                        Inputs = ["request", "workflow"],
                        Outputs = ["planMarkdown", "riskSummary"],
                        CustomMessage = "Focus on delivery risks and keep the plan lean.",
                        Assets = [new WorkflowAssetReference("agent", plannerAssetPath)],
                        Next = "done",
                    },
                    new WorkflowNodeDefinition
                    {
                        Id = "done",
                        Kind = WorkflowNodeKind.Exit,
                        ExitStatus = WorkflowRunStatus.Approved,
                    },
                ],
                EntryPoints = [new WorkflowEntryPointDefinition { Id = "default", NodeId = "planner" }],
                DefaultEntryPoint = "default",
            };

            string prompt = await WorkflowPromptComposer.ComposeAsync(definition, definition.Nodes[0], ["request.md"]);

            Assert.Contains("Role: Planning", prompt, StringComparison.Ordinal);
            Assert.Contains("Agent: planner-agent", prompt, StringComparison.Ordinal);
            Assert.Contains("- request", prompt, StringComparison.Ordinal);
            Assert.Contains("- workflow", prompt, StringComparison.Ordinal);
            Assert.Contains("- planMarkdown", prompt, StringComparison.Ordinal);
            Assert.Contains("- riskSummary", prompt, StringComparison.Ordinal);
            Assert.Contains("- model-plan-fast", prompt, StringComparison.Ordinal);
            Assert.Contains("Focus on delivery risks and keep the plan lean.", prompt, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(tempDirectoryPath, true);
        }
    }

    private static string CreateTempDirectory()
    {
        string tempDirectoryPath = Path.Combine(Path.GetTempPath(), $"clean-squad-prompt-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectoryPath);
        return tempDirectoryPath;
    }
}
