using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CleanSquad.Workflow.Decisions;
using CleanSquad.Workflow.Definitions;
using CleanSquad.Workflow.Orchestration;
using CleanSquad.Workflow.Storage;

namespace CleanSquad.Workflow.UnitTests.Decisions;

/// <summary>
///     Unit tests for <see cref="WorkflowDecisionResolver" />.
/// </summary>
public sealed class WorkflowDecisionResolverTests
{
    /// <summary>
    ///     Verifies legacy review rules approve when the reviewer output is explicitly approved.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task ResolveAsyncApprovesLegacyReviewWhenApprovedAsync()
    {
        WorkflowDecisionResolver resolver = new(new FakeWorkflowAgentRunner([]));
        WorkflowDecisionContext context = CreateContext(
            WorkflowDecisionMode.Rules,
            "legacy-review",
            "Approved: yes\n## Verdict\nShip it.\n");

        WorkflowDecision decision = await resolver.ResolveAsync(context);

        Assert.Equal(WorkflowDecisionAction.Approve, decision.Action);
        Assert.Equal("approve", decision.ChoiceId);
        Assert.Equal("approved", decision.NextNodeId);
    }

    /// <summary>
    ///     Verifies legacy review rules stop when rebuild limits have been reached.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task ResolveAsyncStopsLegacyReviewWhenPolicyLimitIsReachedAsync()
    {
        WorkflowDecisionResolver resolver = new(new FakeWorkflowAgentRunner([]));
        WorkflowDecisionContext context = CreateContext(
            WorkflowDecisionMode.Rules,
            "legacy-review",
            "Approved: no\n## Verdict\nNeeds work.\n",
            state =>
            {
                state.Steps.Add(
                    new WorkflowStepState
                    {
                        StepNumber = 1,
                        NodeId = "rebuilder",
                        Status = WorkflowStepStatus.Completed,
                        StartedAtUtc = TimeProvider.System.GetUtcNow(),
                        CompletedAtUtc = TimeProvider.System.GetUtcNow(),
                    });
            });

        WorkflowDecision decision = await resolver.ResolveAsync(context);

        Assert.Equal(WorkflowDecisionAction.Stop, decision.Action);
        Assert.Equal("stop", decision.ChoiceId);
        Assert.Equal("stopped", decision.NextNodeId);
    }

    /// <summary>
    ///     Verifies agent-mode decisions fall back to the first configured choice when no choice line is emitted.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task ResolveAsyncFallsBackToFirstChoiceWhenAgentOutputIsUnsupportedAsync()
    {
        WorkflowDecisionResolver resolver =
            new(new FakeWorkflowAgentRunner(["# Decision\nNo explicit choice provided."]));
        WorkflowDecisionContext context = CreateContext(WorkflowDecisionMode.Agent, null, string.Empty);

        WorkflowDecision decision = await resolver.ResolveAsync(context);

        Assert.Equal(WorkflowDecisionAction.Branch, decision.Action);
        Assert.Equal("approve", decision.ChoiceId);
        Assert.Equal("approved", decision.NextNodeId);
    }

    /// <summary>
    ///     Verifies agent-mode decisions receive inherited workflow model execution settings.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task ResolveAsyncUsesInheritedAgentSettingsAsync()
    {
        FakeWorkflowAgentRunner runner = new(["Choice: approve"]);
        WorkflowDecisionResolver resolver = new(runner);
        WorkflowDecisionContext context = CreateContext(WorkflowDecisionMode.Agent, null, string.Empty);
        context.Definition.AgentDefaults = new WorkflowAgentDefaultsDefinition
        {
            Models = ["model-decision-default"],
            ReasoningEffort = WorkflowReasoningEffort.High,
            ResponseTimeout = "00:09:00",
        };

        await resolver.ResolveAsync(context);

        AgentCall call = Assert.Single(runner.Calls);
        Assert.Equal(["model-decision-default"], call.ModelIds);
        Assert.Equal(WorkflowReasoningEffort.High, call.ReasoningEffort);
        Assert.Equal(TimeSpan.FromMinutes(9), call.ResponseTimeout);
    }

    /// <summary>
    ///     Verifies clean-agile review rules approve when the master review explicitly approves the phase.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task ResolveAsyncApprovesCleanAgileReviewWhenApprovedAsync()
    {
        WorkflowDecisionResolver resolver = new(new FakeWorkflowAgentRunner([]));
        WorkflowDecisionContext context = CreateContext(
            WorkflowDecisionMode.Rules,
            "clean-agile-review",
            "Approved: yes\n## Consolidated Assessment\nReady to proceed.\n",
            configureNode: node => node.DecisionSourceNodeId = "phase-master-review");

        WorkflowDecision decision = await resolver.ResolveAsync(context);

        Assert.Equal(WorkflowDecisionAction.Approve, decision.Action);
        Assert.Equal("approve", decision.ChoiceId);
        Assert.Equal("approved", decision.NextNodeId);
    }

    /// <summary>
    ///     Verifies clean-agile review rules request phase rework when approval is denied and a rework path exists.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task ResolveAsyncRequestsReworkForCleanAgileReviewWhenDeniedAsync()
    {
        WorkflowDecisionResolver resolver = new(new FakeWorkflowAgentRunner([]));
        WorkflowDecisionContext context = CreateContext(
            WorkflowDecisionMode.Rules,
            "clean-agile-review",
            "Approved: no\n## Consolidated Assessment\nArchitecture needs refinement.\n",
            state => state.Steps.Add(
                new WorkflowStepState
                {
                    StepNumber = 1,
                    NodeId = "phase-master-review",
                    Status = WorkflowStepStatus.Completed,
                    StartedAtUtc = TimeProvider.System.GetUtcNow(),
                    CompletedAtUtc = TimeProvider.System.GetUtcNow(),
                }),
            node =>
            {
                node.DecisionSourceNodeId = "phase-master-review";
                node.Choices =
                [
                    new WorkflowDecisionOptionDefinition { Id = "approve", NextNodeId = "approved" },
                    new WorkflowDecisionOptionDefinition { Id = "rework", NextNodeId = "three-amigos-fork" },
                    new WorkflowDecisionOptionDefinition { Id = "stop", NextNodeId = "stopped" },
                ];
            });

        WorkflowDecision decision = await resolver.ResolveAsync(context);

        Assert.Equal(WorkflowDecisionAction.Branch, decision.Action);
        Assert.Equal("rework", decision.ChoiceId);
        Assert.Equal("three-amigos-fork", decision.NextNodeId);
    }

    /// <summary>
    ///     Verifies clean-agile review rules stop when rebuild limits have already been consumed.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task ResolveAsyncStopsCleanAgileReviewWhenRebuildLimitIsReachedAsync()
    {
        WorkflowDecisionResolver resolver = new(new FakeWorkflowAgentRunner([]));
        WorkflowDecisionContext context = CreateContext(
            WorkflowDecisionMode.Rules,
            "clean-agile-review",
            "Approved: no\n## Consolidated Assessment\nCode still needs work.\n",
            state =>
            {
                state.Steps.Add(
                    new WorkflowStepState
                    {
                        StepNumber = 1,
                        NodeId = "code-master-review",
                        Status = WorkflowStepStatus.Completed,
                        StartedAtUtc = TimeProvider.System.GetUtcNow(),
                        CompletedAtUtc = TimeProvider.System.GetUtcNow(),
                    });
                state.Steps.Add(
                    new WorkflowStepState
                    {
                        StepNumber = 2,
                        NodeId = "rebuilder",
                        Status = WorkflowStepStatus.Completed,
                        StartedAtUtc = TimeProvider.System.GetUtcNow(),
                        CompletedAtUtc = TimeProvider.System.GetUtcNow(),
                    });
            },
            node => node.DecisionSourceNodeId = "code-master-review");

        WorkflowDecision decision = await resolver.ResolveAsync(context);

        Assert.Equal(WorkflowDecisionAction.Stop, decision.Action);
        Assert.Equal("stop", decision.ChoiceId);
        Assert.Equal("stopped", decision.NextNodeId);
    }

    /// <summary>
    ///     Verifies code review uses the rebuild limit instead of the planning-review cycle limit.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task ResolveAsyncAllowsConfiguredRebuildAfterTwoCodeReviewsAsync()
    {
        WorkflowDecisionResolver resolver = new(new FakeWorkflowAgentRunner([]));
        WorkflowDecisionContext context = CreateContext(
            WorkflowDecisionMode.Rules,
            "clean-agile-review",
            "Approved: no\n## Consolidated Assessment\nCode needs another focused correction.\n",
            state =>
            {
                for (int stepNumber = 1; stepNumber <= 2; stepNumber++)
                {
                    state.Steps.Add(
                        new WorkflowStepState
                        {
                            StepNumber = stepNumber,
                            NodeId = "code-master-review",
                            Status = WorkflowStepStatus.Completed,
                            StartedAtUtc = TimeProvider.System.GetUtcNow(),
                            CompletedAtUtc = TimeProvider.System.GetUtcNow(),
                        });
                }

                state.Steps.Add(
                    new WorkflowStepState
                    {
                        StepNumber = 3,
                        NodeId = "rebuilder",
                        Status = WorkflowStepStatus.Completed,
                        StartedAtUtc = TimeProvider.System.GetUtcNow(),
                        CompletedAtUtc = TimeProvider.System.GetUtcNow(),
                    });
            },
            node => node.DecisionSourceNodeId = "code-master-review",
            definition => definition.Policy.MaxRebuilds = 2);

        WorkflowDecision decision = await resolver.ResolveAsync(context);

        Assert.Equal(WorkflowDecisionAction.Rebuild, decision.Action);
        Assert.Equal("rebuild", decision.ChoiceId);
        Assert.Equal("rebuilder", decision.NextNodeId);
    }

    private static WorkflowDecisionContext CreateContext(
        WorkflowDecisionMode decisionMode,
        string? ruleSet,
        string sourceMarkdown,
        Action<WorkflowRunState>? configureState = null,
        Action<WorkflowNodeDefinition>? configureNode = null,
        Action<WorkflowDefinition>? configureDefinition = null)
    {
        string tempDirectoryPath = Path.Combine(Path.GetTempPath(), $"clean-squad-decision-{Guid.NewGuid():N}");
        WorkflowDefinition definition = new()
        {
            Name = "Decision Test Workflow",
            Policy = new WorkflowPolicyDefinition
            {
                DecisionMode = decisionMode,
                MaxRebuilds = 1,
                MaxReviewCycles = 2,
            },
        };
        configureDefinition?.Invoke(definition);
        WorkflowNodeDefinition node = new()
        {
            Id = "review-decision",
            Kind = WorkflowNodeKind.Decision,
            Role = "Decision",
            DecisionMode = decisionMode,
            RuleSet = ruleSet,
            Choices =
            [
                new WorkflowDecisionOptionDefinition { Id = "approve", NextNodeId = "approved" },
                new WorkflowDecisionOptionDefinition { Id = "rebuild", NextNodeId = "rebuilder" },
                new WorkflowDecisionOptionDefinition { Id = "stop", NextNodeId = "stopped" },
            ],
        };
        configureNode?.Invoke(node);
        WorkflowArtifacts artifacts = WorkflowArtifacts.Create(
            tempDirectoryPath,
            Path.Combine(tempDirectoryPath, "workflow.json"),
            Path.Combine(tempDirectoryPath, "request.md"),
            TimeProvider.System);
        WorkflowRunState state = WorkflowRunState.Create("run-1", definition.Name, "planner", TimeProvider.System);
        state.PendingActivations.Clear();
        configureState?.Invoke(state);

        return new WorkflowDecisionContext(
            definition,
            node,
            artifacts,
            state,
            [],
            sourceMarkdown);
    }

    private sealed class FakeWorkflowAgentRunner : IWorkflowAgentRunner
    {
        private readonly Queue<string> responses;

        public FakeWorkflowAgentRunner(IEnumerable<string> responses)
        {
            this.responses = new Queue<string>(responses);
        }

        public List<AgentCall> Calls { get; } = [];

        public Task<string> RunAsync(
            string agentName,
            string prompt,
            IReadOnlyList<string> attachmentFilePaths,
            IReadOnlyList<string> modelIds,
            string? reasoningEffort,
            TimeSpan? responseTimeout = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls.Add(new AgentCall(modelIds.ToArray(), reasoningEffort, responseTimeout));
            return Task.FromResult(responses.Dequeue());
        }
    }

    private sealed record AgentCall(
        IReadOnlyList<string> ModelIds,
        string? ReasoningEffort,
        TimeSpan? ResponseTimeout);
}
