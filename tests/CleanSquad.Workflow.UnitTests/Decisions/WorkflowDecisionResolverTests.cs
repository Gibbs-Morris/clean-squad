using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CleanSquad.Workflow;
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
                state.Steps.Add(new WorkflowStepState
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
        WorkflowDecisionResolver resolver = new(new FakeWorkflowAgentRunner(["# Decision\nNo explicit choice provided."]));
        WorkflowDecisionContext context = CreateContext(WorkflowDecisionMode.Agent, null, string.Empty);

        WorkflowDecision decision = await resolver.ResolveAsync(context);

        Assert.Equal(WorkflowDecisionAction.Branch, decision.Action);
        Assert.Equal("approve", decision.ChoiceId);
        Assert.Equal("approved", decision.NextNodeId);
    }

    private static WorkflowDecisionContext CreateContext(
        WorkflowDecisionMode decisionMode,
        string? ruleSet,
        string sourceMarkdown,
        Action<WorkflowRunState>? configureState = null)
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

        public Task<string> RunAsync(
            string agentName,
            string prompt,
            IReadOnlyList<string> attachmentFilePaths,
            IReadOnlyList<string> modelIds,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(this.responses.Dequeue());
        }
    }
}
