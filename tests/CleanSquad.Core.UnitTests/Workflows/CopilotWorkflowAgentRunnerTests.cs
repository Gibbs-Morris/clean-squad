using CleanSquad.Core.Workflows;
using CleanSquad.Workflow;
using GitHub.Copilot;

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
    [Fact]
    public void CreateSessionConfigUsesFirstModelAndApprovesWritePermissions()
    {
        SessionConfig config = CopilotWorkflowAgentRunner.CreateSessionConfig(
            @"c:\repo",
            "gpt-5.4",
            WorkflowReasoningEffort.High);

        Assert.Equal(@"c:\repo", config.WorkingDirectory);
        Assert.Equal("gpt-5.4", config.Model);
        Assert.Equal(WorkflowReasoningEffort.High, config.ReasoningEffort);
        Assert.Same(PermissionHandler.ApproveAll, config.OnPermissionRequest);
    }

    /// <summary>
    ///     Verifies the workflow runner also approves shell commands so the builder can run validation commands
    ///     like build and test during implementation.
    /// </summary>
    [Fact]
    public void CreateSessionConfigApprovesShellPermissions()
    {
        SessionConfig config = CopilotWorkflowAgentRunner.CreateSessionConfig(
            @"c:\repo",
            null,
            null);

        Assert.Null(config.Model);
        Assert.Null(config.ReasoningEffort);
        Assert.Same(PermissionHandler.ApproveAll, config.OnPermissionRequest);
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

    /// <summary>
    ///     Verifies model preferences select the first configured model available to the current Copilot account.
    /// </summary>
    [Fact]
    public void SelectPreferredAvailableModelUsesOrderedFallback()
    {
        string? selectedModel = CopilotWorkflowAgentRunner.SelectPreferredAvailableModelId(
            ["model-unavailable", "model-fallback", "model-last"],
            ["model-last", "model-fallback"]);

        Assert.Equal("model-fallback", selectedModel);
    }

    /// <summary>
    ///     Verifies an explicit auto preference remains a valid provider-selected model choice.
    /// </summary>
    [Fact]
    public void SelectPreferredAvailableModelSupportsAuto()
    {
        string? selectedModel = CopilotWorkflowAgentRunner.SelectPreferredAvailableModelId(
            ["model-unavailable", "auto"],
            []);

        Assert.Equal("auto", selectedModel);
    }

    /// <summary>
    ///     Verifies model matching ignores identifier casing while retaining the configured identifier.
    /// </summary>
    [Fact]
    public void SelectPreferredAvailableModelMatchesCaseInsensitively()
    {
        string? selectedModel = CopilotWorkflowAgentRunner.SelectPreferredAvailableModelId(
            ["MODEL-PRIMARY"],
            ["model-primary"]);

        Assert.Equal("MODEL-PRIMARY", selectedModel);
    }

    /// <summary>
    ///     Verifies unavailable configured models do not silently fall back to an unrelated provider default.
    /// </summary>
    [Fact]
    public void SelectPreferredAvailableModelReturnsNullWhenNoPreferenceIsAvailable()
    {
        string? selectedModel = CopilotWorkflowAgentRunner.SelectPreferredAvailableModelId(
            ["model-unavailable"],
            ["model-other"]);

        Assert.Null(selectedModel);
    }
}
