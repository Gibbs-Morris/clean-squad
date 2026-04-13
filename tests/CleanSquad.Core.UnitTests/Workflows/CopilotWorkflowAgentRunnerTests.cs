using System.Threading.Tasks;
using CleanSquad.Core.Workflows;
using CleanSquad.Workflow;
using GitHub.Copilot.SDK;

namespace CleanSquad.Core.UnitTests.Workflows;

/// <summary>
///     Unit tests for <see cref="CopilotWorkflowAgentRunner" /> session configuration.
/// </summary>
public sealed class CopilotWorkflowAgentRunnerTests
{
    /// <summary>
    ///     Verifies the workflow runner creates a session configuration that honors the first requested model
    ///     and approves file-write permissions needed for implementation stages.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task CreateSessionConfigUsesFirstModelAndApprovesWritePermissionsAsync()
    {
        SessionConfig config = CopilotWorkflowAgentRunner.CreateSessionConfig(
            @"c:\repo",
            "gpt-5.4",
            WorkflowReasoningEffort.High);

        PermissionRequestResult result = await config.OnPermissionRequest!(
            new PermissionRequestWrite
            {
                ToolCallId = "tool-1",
                Intention = "Update the README",
                FileName = "README.md",
                Diff = "@@ -1 +1 @@",
            },
            new PermissionInvocation
            {
                SessionId = "session-1",
            });

        Assert.Equal(@"c:\repo", config.WorkingDirectory);
        Assert.Equal("gpt-5.4", config.Model);
        Assert.Equal(WorkflowReasoningEffort.High, config.ReasoningEffort);
        Assert.Equal(PermissionRequestResultKind.Approved, result.Kind);
    }

    /// <summary>
    ///     Verifies the workflow runner also approves shell commands so the builder can run validation commands
    ///     like build and test during implementation.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task CreateSessionConfigApprovesShellPermissionsAsync()
    {
        SessionConfig config = CopilotWorkflowAgentRunner.CreateSessionConfig(
            @"c:\repo",
            null,
            null);

        PermissionRequestResult result = await config.OnPermissionRequest!(
            new PermissionRequestShell
            {
                ToolCallId = "tool-2",
                Intention = "Run the solution tests",
                FullCommandText = "dotnet test .\\CleanSquad.slnx",
                Commands = [],
                PossiblePaths = [],
                PossibleUrls = [],
                HasWriteFileRedirection = false,
                CanOfferSessionApproval = true,
            },
            new PermissionInvocation
            {
                SessionId = "session-2",
            });

        Assert.Null(config.Model);
        Assert.Null(config.ReasoningEffort);
        Assert.Equal(PermissionRequestResultKind.Approved, result.Kind);
    }

    /// <summary>
    ///     Verifies the workflow runner can resolve the strongest supported reasoning effort for the selected model.
    /// </summary>
    [Fact]
    public void ResolveHighestSupportedReasoningEffortReturnsStrongestSupportedValue()
    {
        string? resolvedReasoningEffort = CopilotWorkflowAgentRunner.ResolveHighestSupportedReasoningEffort(
            [WorkflowReasoningEffort.Medium, WorkflowReasoningEffort.ExtraHigh, WorkflowReasoningEffort.High]);

        Assert.Equal(WorkflowReasoningEffort.ExtraHigh, resolvedReasoningEffort);
    }
}
