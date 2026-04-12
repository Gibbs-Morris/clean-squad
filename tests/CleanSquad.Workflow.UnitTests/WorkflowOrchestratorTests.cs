using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CleanSquad.Workflow;
using CleanSquad.Workflow.Decisions;
using CleanSquad.Workflow.Definitions;
using CleanSquad.Workflow.Orchestration;
using CleanSquad.Workflow.Storage;
using CleanSquad.Workflow.UnitTests.TestFixtures;

namespace CleanSquad.Workflow.UnitTests;

/// <summary>
///     Unit tests for <see cref="WorkflowOrchestrator" />.
/// </summary>
public sealed class WorkflowOrchestratorTests
{
    /// <summary>
    ///     Verifies the workflow approves after a successful review using the legacy definition shape.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task ExecuteAsyncApprovesWhenReviewerApprovesAsync()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            string definitionPath = await CreateLegacyWorkflowDefinitionAsync(tempDirectoryPath, WorkflowDecisionMode.Rules);
            string requestPath = Path.Combine(tempDirectoryPath, "request.md");
            await File.WriteAllTextAsync(requestPath, "# Request\n", System.Text.Encoding.UTF8);

            FakeWorkflowAgentRunner runner = new([
                "# Plan\n## Goal\nShip it\n",
                "# Build\n## Summary\nBuild summary\n",
                "# Review\nApproved: yes\n## Verdict\nLooks good.\n## Findings\n- None.\n## Builder Instructions\n- None.\n",
            ]);
            MarkdownArtifactService artifactService = new(new FixedTimeProvider(new DateTimeOffset(2026, 4, 11, 14, 0, 0, TimeSpan.Zero)));
            WorkflowOrchestrator orchestrator = new(
                artifactService,
                runner,
                new WorkflowDecisionResolver(runner));

            WorkflowRunResult result = await orchestrator.ExecuteAsync(tempDirectoryPath, definitionPath, requestPath);

            Assert.Equal(WorkflowRunStatus.Approved, result.Status);
            Assert.Equal(1, result.ReviewCycleCount);
            Assert.Equal(0, result.RebuildCount);
            Assert.Equal(3, runner.Calls.Count);
            Assert.True(File.Exists(result.FinalArtifactPath));
            Assert.True(File.Exists(Path.Combine(result.RunDirectoryPath, "state.json")));
        }
        finally
        {
            Directory.Delete(tempDirectoryPath, true);
        }
    }

    /// <summary>
    ///     Verifies a graph workflow can start from a named entry point and complete after a fork/join phase.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task ExecuteAsyncSupportsEntryPointsAndParallelJoinAsync()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            string definitionPath = await CreateGraphWorkflowDefinitionAsync(tempDirectoryPath);
            string requestPath = Path.Combine(tempDirectoryPath, "request.md");
            await File.WriteAllTextAsync(requestPath, "# Request\n", System.Text.Encoding.UTF8);

            FakeWorkflowAgentRunner runner = new([
                "# Research\n## Code Insights\n- Code path\n",
                "# Research\n## Test Insights\n- Test path\n",
                "# Build\n## Summary\nBuild summary\n",
                "# Review\nApproved: yes\n## Verdict\nLooks good.\n## Findings\n- None.\n## Builder Instructions\n- None.\n",
            ]);
            MarkdownArtifactService artifactService = new(new FixedTimeProvider(new DateTimeOffset(2026, 4, 11, 15, 0, 0, TimeSpan.Zero)));
            WorkflowOrchestrator orchestrator = new(
                artifactService,
                runner,
                new WorkflowDecisionResolver(runner));

            WorkflowRunResult result = await orchestrator.ExecuteAsync(
                new WorkflowExecutionRequest(
                    tempDirectoryPath,
                    definitionPath,
                    requestPath,
                    "research"));

            WorkflowArtifacts artifacts = artifactService.LoadRunArtifacts(result.RunDirectoryPath);
            WorkflowRunState state = artifactService.ReadState(artifacts);

            Assert.Equal(WorkflowRunStatus.Approved, result.Status);
            Assert.Equal(2, runner.Calls.Count(call => string.Equals(call.AgentName, "Research", StringComparison.Ordinal)));
            Assert.Contains(runner.Calls, call => string.Equals(call.AgentName, "Research", StringComparison.Ordinal) && call.ModelIds.SequenceEqual(["model-code-fast"]));
            Assert.Contains(runner.Calls, call => string.Equals(call.AgentName, "Research", StringComparison.Ordinal) && call.ModelIds.SequenceEqual(["model-test-deep"]));
            Assert.Equal(4, runner.Calls.Count);
            Assert.True(File.Exists(Path.Combine(result.RunDirectoryPath, "events.ndjson")));
            Assert.Contains(state.Steps, step => string.Equals(step.NodeId, "code-research", StringComparison.Ordinal) && step.Models.SequenceEqual(["model-code-fast"]));
            Assert.Contains(state.Steps, step => string.Equals(step.NodeId, "test-research", StringComparison.Ordinal) && step.Models.SequenceEqual(["model-test-deep"]));
        }
        finally
        {
            Directory.Delete(tempDirectoryPath, true);
        }
    }

    /// <summary>
    ///     Verifies a timed-out stage in a parallel wave is captured as a resumable workflow failure.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task ExecuteAsyncCapturesTimedOutParallelStageForResumeAsync()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            string definitionPath = await CreateGraphWorkflowDefinitionAsync(tempDirectoryPath);
            string requestPath = Path.Combine(tempDirectoryPath, "request.md");
            await File.WriteAllTextAsync(requestPath, "# Request\n", System.Text.Encoding.UTF8);

            FakeWorkflowAgentRunner failingRunner = new(
                [
                    "# Research\n## Code Insights\n- Code path\n",
                ],
                exceptionAtCall: 2,
                exceptionFactory: static (_, _) => new TimeoutException("SendAndWaitAsync timed out after 00:01:00"));
            MarkdownArtifactService artifactService = new(new FixedTimeProvider(new DateTimeOffset(2026, 4, 12, 20, 0, 0, TimeSpan.Zero)));
            WorkflowOrchestrator failingOrchestrator = new(
                artifactService,
                failingRunner,
                new WorkflowDecisionResolver(failingRunner));

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                failingOrchestrator.ExecuteAsync(tempDirectoryPath, definitionPath, requestPath));

            string runDirectoryPath = Directory.GetDirectories(Path.Combine(tempDirectoryPath, ".workflow-testing", "workflow-runs")).Single();
            WorkflowArtifacts artifacts = artifactService.LoadRunArtifacts(runDirectoryPath);
            WorkflowRunState failedState = artifactService.ReadState(artifacts);
            WorkflowStepState codeResearchStep = Assert.Single(failedState.Steps, step => string.Equals(step.NodeId, "code-research", StringComparison.Ordinal));
            WorkflowStepState testResearchStep = Assert.Single(failedState.Steps, step => string.Equals(step.NodeId, "test-research", StringComparison.Ordinal));
            WorkflowStepState failedResearchStep = Assert.Single(
              [codeResearchStep, testResearchStep],
              step => step.Status == WorkflowStepStatus.Failed);
            WorkflowStepState completedResearchStep = Assert.Single(
              [codeResearchStep, testResearchStep],
              step => step.Status == WorkflowStepStatus.Completed);

            Assert.Equal("SendAndWaitAsync timed out after 00:01:00", exception.Message);
            Assert.IsType<TimeoutException>(exception.InnerException);
            Assert.Equal(WorkflowRunStatus.Failed, failedState.Status);
            Assert.Equal("SendAndWaitAsync timed out after 00:01:00", failedResearchStep.Message);
            Assert.NotNull(completedResearchStep.CompletedAtUtc);

            FakeWorkflowAgentRunner resumeRunner = new([
                "# Research\n## Test Insights\n- Recovered test path\n",
                "# Build\n## Summary\nRecovered build\n",
                "# Review\nApproved: yes\n## Verdict\nLooks good.\n## Findings\n- None.\n## Builder Instructions\n- None.\n",
            ]);
            WorkflowOrchestrator resumeOrchestrator = new(
                artifactService,
                resumeRunner,
                new WorkflowDecisionResolver(resumeRunner));

            WorkflowRunResult resumedResult = await resumeOrchestrator.ExecuteAsync(
                new WorkflowExecutionRequest(
                    tempDirectoryPath,
                    null,
                    null,
                    ResumePath: runDirectoryPath));

            Assert.Equal(WorkflowRunStatus.Approved, resumedResult.Status);
            Assert.Equal(3, resumeRunner.Calls.Count);
        }
        finally
        {
            Directory.Delete(tempDirectoryPath, true);
        }
      }

    /// <summary>
    ///     Verifies a stage can declare an explicit agent, custom message, inputs, outputs, and model.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task ExecuteAsyncUsesExplicitAgentAndStageMetadataAsync()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            string definitionPath = await CreateAgentConfiguredWorkflowDefinitionAsync(tempDirectoryPath);
            string requestPath = Path.Combine(tempDirectoryPath, "request.md");
            await File.WriteAllTextAsync(requestPath, "# Request\n", System.Text.Encoding.UTF8);

            FakeWorkflowAgentRunner runner = new([
            "# Plan\n## Goal\nPlan the change\n",
                "# Build\n## Summary\nCompleted\n",
            ]);
            MarkdownArtifactService artifactService = new(new FixedTimeProvider(new DateTimeOffset(2026, 4, 11, 17, 0, 0, TimeSpan.Zero)));
            WorkflowOrchestrator orchestrator = new(
                artifactService,
                runner,
                new WorkflowDecisionResolver(runner));

            WorkflowRunResult result = await orchestrator.ExecuteAsync(tempDirectoryPath, definitionPath, requestPath);
            WorkflowArtifacts artifacts = artifactService.LoadRunArtifacts(result.RunDirectoryPath);
            WorkflowRunState state = artifactService.ReadState(artifacts);
            AgentCall call = Assert.Single(runner.Calls, call => string.Equals(call.AgentName, "builder-agent", StringComparison.Ordinal));
            WorkflowStepState step = Assert.Single(state.Steps, step => string.Equals(step.NodeId, "builder", StringComparison.OrdinalIgnoreCase));

            Assert.Equal(WorkflowRunStatus.Approved, result.Status);
            Assert.Equal(2, runner.Calls.Count);
            Assert.Equal("builder-agent", call.AgentName);
            Assert.Contains("Focus on the requested file only.", call.Prompt, StringComparison.Ordinal);
            Assert.Contains("- node:planner", call.Prompt, StringComparison.Ordinal);
            Assert.Contains("- buildSummary", call.Prompt, StringComparison.Ordinal);
            Assert.Equal("Builder", step.RoleName);
            Assert.Equal("builder-agent", step.AgentName);
            Assert.Equal(["request", "node:planner"], step.InputReferences);
            Assert.Equal(["buildSummary"], step.OutputNames);
            Assert.Equal("Focus on the requested file only.", step.CustomMessage);
            Assert.Equal(["model-build-fast"], step.Models);
        }
        finally
        {
            Directory.Delete(tempDirectoryPath, true);
        }
    }

    /// <summary>
    ///     Verifies a failed workflow can be resumed from persisted state.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task ExecuteAsyncResumesFailedWorkflowAsync()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            string definitionPath = await CreateLegacyWorkflowDefinitionAsync(tempDirectoryPath, WorkflowDecisionMode.Rules);
            string requestPath = Path.Combine(tempDirectoryPath, "request.md");
            await File.WriteAllTextAsync(requestPath, "# Request\n", System.Text.Encoding.UTF8);

            FakeWorkflowAgentRunner failingRunner = new(
                [
                    "# Plan\n## Goal\nShip it\n",
                ],
                exceptionAtCall: 2);
            MarkdownArtifactService artifactService = new(new FixedTimeProvider(new DateTimeOffset(2026, 4, 11, 16, 0, 0, TimeSpan.Zero)));
            WorkflowOrchestrator failingOrchestrator = new(
                artifactService,
                failingRunner,
                new WorkflowDecisionResolver(failingRunner));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                failingOrchestrator.ExecuteAsync(tempDirectoryPath, definitionPath, requestPath));

            string runDirectoryPath = Directory.GetDirectories(Path.Combine(tempDirectoryPath, ".workflow-testing", "workflow-runs")).Single();

            FakeWorkflowAgentRunner resumeRunner = new([
                "# Build\n## Summary\nRecovered build\n",
                "# Review\nApproved: yes\n## Verdict\nLooks good.\n## Findings\n- None.\n## Builder Instructions\n- None.\n",
            ]);
            WorkflowOrchestrator resumeOrchestrator = new(
                artifactService,
                resumeRunner,
                new WorkflowDecisionResolver(resumeRunner));

            WorkflowRunResult resumedResult = await resumeOrchestrator.ExecuteAsync(
                new WorkflowExecutionRequest(
                    tempDirectoryPath,
                    null,
                    null,
                    ResumePath: runDirectoryPath));

            Assert.Equal(WorkflowRunStatus.Approved, resumedResult.Status);
            Assert.Equal(2, resumeRunner.Calls.Count);
            Assert.True(File.Exists(Path.Combine(runDirectoryPath, "state.json")));
        }
        finally
        {
            Directory.Delete(tempDirectoryPath, true);
        }
    }

    private static async Task<string> CreateLegacyWorkflowDefinitionAsync(string tempDirectoryPath, WorkflowDecisionMode decisionMode)
    {
        string assetsDirectoryPath = Path.Combine(tempDirectoryPath, "assets");
        Directory.CreateDirectory(assetsDirectoryPath);
        await File.WriteAllTextAsync(Path.Combine(assetsDirectoryPath, "shared.md"), "shared", System.Text.Encoding.UTF8);
        await File.WriteAllTextAsync(Path.Combine(assetsDirectoryPath, "planner.md"), "planner", System.Text.Encoding.UTF8);
        await File.WriteAllTextAsync(Path.Combine(assetsDirectoryPath, "builder.md"), "builder", System.Text.Encoding.UTF8);
        await File.WriteAllTextAsync(Path.Combine(assetsDirectoryPath, "reviewer.md"), "reviewer", System.Text.Encoding.UTF8);
        await File.WriteAllTextAsync(Path.Combine(assetsDirectoryPath, "rebuilder.md"), "rebuilder", System.Text.Encoding.UTF8);
        await File.WriteAllTextAsync(Path.Combine(assetsDirectoryPath, "decision.md"), "decision", System.Text.Encoding.UTF8);
        string definitionPath = Path.Combine(tempDirectoryPath, "workflow.json");
        string definitionJson = $@"{{
      ""name"": ""Test Workflow"",
      ""sharedAssets"": [{{ ""kind"": ""instruction"", ""path"": ""assets/shared.md"" }}],
      ""planner"": {{ ""displayName"": ""Planner"", ""models"": [""model-plan""], ""assets"": [{{ ""kind"": ""agent"", ""path"": ""assets/planner.md"" }}] }},
      ""builder"": {{ ""displayName"": ""Builder"", ""models"": [""model-build""], ""assets"": [{{ ""kind"": ""agent"", ""path"": ""assets/builder.md"" }}] }},
      ""reviewer"": {{ ""displayName"": ""Reviewer"", ""models"": [""model-review""], ""assets"": [{{ ""kind"": ""agent"", ""path"": ""assets/reviewer.md"" }}] }},
      ""decision"": {{ ""displayName"": ""Decision"", ""models"": [""model-decision""], ""assets"": [{{ ""kind"": ""agent"", ""path"": ""assets/decision.md"" }}] }},
      ""rebuilder"": {{ ""displayName"": ""Rebuilder"", ""models"": [""model-rebuild""], ""assets"": [{{ ""kind"": ""agent"", ""path"": ""assets/rebuilder.md"" }}] }},
      ""policy"": {{ ""decisionMode"": ""{decisionMode}"", ""maxReviewCycles"": 2, ""maxRebuilds"": 1 }}
    }}";
        await File.WriteAllTextAsync(definitionPath, definitionJson, System.Text.Encoding.UTF8);
        return definitionPath;
    }

    private static async Task<string> CreateGraphWorkflowDefinitionAsync(string tempDirectoryPath)
    {
        string assetsDirectoryPath = Path.Combine(tempDirectoryPath, "assets");
        Directory.CreateDirectory(assetsDirectoryPath);
        await File.WriteAllTextAsync(Path.Combine(assetsDirectoryPath, "shared.md"), "shared", System.Text.Encoding.UTF8);
        await File.WriteAllTextAsync(Path.Combine(assetsDirectoryPath, "code.md"), "code research", System.Text.Encoding.UTF8);
        await File.WriteAllTextAsync(Path.Combine(assetsDirectoryPath, "tests.md"), "test research", System.Text.Encoding.UTF8);
        await File.WriteAllTextAsync(Path.Combine(assetsDirectoryPath, "builder.md"), "builder", System.Text.Encoding.UTF8);
        await File.WriteAllTextAsync(Path.Combine(assetsDirectoryPath, "reviewer.md"), "reviewer", System.Text.Encoding.UTF8);
        string definitionPath = Path.Combine(tempDirectoryPath, "workflow.graph.json");
        string definitionJson = """
{
  "name": "Graph Workflow",
  "defaultEntryPoint": "default",
  "entryPoints": [
    { "id": "default", "nodeId": "research-fork" },
    { "id": "research", "nodeId": "research-fork" }
  ],
  "sharedAssets": [
    { "kind": "instruction", "path": "assets/shared.md" }
  ],
  "nodes": [
    {
      "id": "research-fork",
      "kind": "Fork",
      "joinNodeId": "research-join",
      "branches": [
        { "id": "code", "nextNodeId": "code-research" },
        { "id": "testing", "nextNodeId": "test-research" }
      ]
    },
    {
      "id": "code-research",
      "kind": "Stage",
      "role": "Research",
      "models": ["model-code-fast"],
      "inputs": ["request"],
      "assets": [
        { "kind": "agent", "path": "assets/code.md" }
      ],
      "next": "research-join"
    },
    {
      "id": "test-research",
      "kind": "Stage",
      "role": "Research",
      "models": ["model-test-deep"],
      "inputs": ["request"],
      "assets": [
        { "kind": "agent", "path": "assets/tests.md" }
      ],
      "next": "research-join"
    },
    {
      "id": "research-join",
      "kind": "Join",
      "forkId": "research-fork",
      "next": "builder"
    },
    {
      "id": "builder",
      "kind": "Stage",
      "role": "Builder",
      "inputs": ["request", "node:code-research", "node:test-research"],
      "assets": [
        { "kind": "agent", "path": "assets/builder.md" }
      ],
      "next": "reviewer"
    },
    {
      "id": "reviewer",
      "kind": "Stage",
      "role": "Reviewer",
      "inputs": ["request", "node:builder"],
      "assets": [
        { "kind": "agent", "path": "assets/reviewer.md" }
      ],
      "next": "review-decision"
    },
    {
      "id": "review-decision",
      "kind": "Decision",
      "role": "Decision",
      "decisionMode": "Rules",
      "ruleSet": "legacy-review",
      "decisionSourceNodeId": "reviewer",
      "choices": [
        { "id": "approve", "nextNodeId": "approved" },
        { "id": "stop", "nextNodeId": "stopped" },
        { "id": "rebuild", "nextNodeId": "builder" }
      ]
    },
    {
      "id": "approved",
      "kind": "Exit",
      "exitStatus": "Approved"
    },
    {
      "id": "stopped",
      "kind": "Exit",
      "exitStatus": "Stopped"
    }
  ],
  "policy": {
    "decisionMode": "Rules",
    "maxReviewCycles": 2,
    "maxRebuilds": 1,
    "maxParallelism": 2,
    "maxNodeVisits": 8
  }
}
""";
        await File.WriteAllTextAsync(definitionPath, definitionJson, System.Text.Encoding.UTF8);
        return definitionPath;
    }

    private static async Task<string> CreateAgentConfiguredWorkflowDefinitionAsync(string tempDirectoryPath)
    {
        string assetsDirectoryPath = Path.Combine(tempDirectoryPath, "assets");
        Directory.CreateDirectory(assetsDirectoryPath);
        await File.WriteAllTextAsync(Path.Combine(assetsDirectoryPath, "planner.md"), "planner", System.Text.Encoding.UTF8);
        await File.WriteAllTextAsync(Path.Combine(assetsDirectoryPath, "builder.md"), "builder", System.Text.Encoding.UTF8);
        string definitionPath = Path.Combine(tempDirectoryPath, "workflow.agent.json");
        string definitionJson = """
{
  "name": "Agent Config Workflow",
  "defaultEntryPoint": "default",
  "entryPoints": [
    { "id": "default", "nodeId": "planner" }
  ],
  "nodes": [
    {
      "id": "planner",
      "kind": "Stage",
      "role": "Planner",
      "assets": [
        { "kind": "agent", "path": "assets/planner.md" }
      ],
      "next": "builder"
    },
    {
      "id": "builder",
      "kind": "Stage",
      "role": "Builder",
      "agent": "builder-agent",
      "models": ["model-build-fast"],
      "inputs": ["request", "node:planner"],
      "outputs": ["buildSummary"],
      "customMessage": "Focus on the requested file only.",
      "assets": [
        { "kind": "agent", "path": "assets/builder.md" }
      ],
      "next": "approved"
    },
    {
      "id": "approved",
      "kind": "Exit",
      "exitStatus": "Approved"
    }
  ],
  "policy": {
    "decisionMode": "Rules",
    "maxReviewCycles": 2,
    "maxRebuilds": 1,
    "maxParallelism": 1,
    "maxNodeVisits": 8
  }
}
""";
        await File.WriteAllTextAsync(definitionPath, definitionJson, System.Text.Encoding.UTF8);
        return definitionPath;
    }

    private static string CreateTempDirectory()
    {
        string tempDirectoryPath = Path.Combine(Path.GetTempPath(), $"clean-squad-orchestrator-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectoryPath);
        return tempDirectoryPath;
    }

    private sealed class FakeWorkflowAgentRunner : IWorkflowAgentRunner
    {
        private readonly Queue<string> responses;
        private readonly int? exceptionAtCall;
        private readonly Func<int, string, Exception>? exceptionFactory;

        public FakeWorkflowAgentRunner(
            IEnumerable<string> responses,
            int? exceptionAtCall = null,
            Func<int, string, Exception>? exceptionFactory = null)
        {
            this.responses = new Queue<string>(responses);
            this.exceptionAtCall = exceptionAtCall;
            this.exceptionFactory = exceptionFactory;
        }

        public List<AgentCall> Calls { get; } = [];

        public Task<string> RunAsync(
            string agentName,
            string prompt,
            IReadOnlyList<string> attachmentFilePaths,
            IReadOnlyList<string> modelIds,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.Calls.Add(new AgentCall(agentName, prompt, attachmentFilePaths, modelIds.ToArray()));
            if (this.exceptionAtCall.HasValue && this.Calls.Count == this.exceptionAtCall.Value)
            {
              throw this.exceptionFactory?.Invoke(this.Calls.Count, agentName) ?? new InvalidOperationException($"Boom on {agentName}");
            }

            return Task.FromResult(this.responses.Dequeue());
        }
    }

    private sealed record AgentCall(string AgentName, string Prompt, IReadOnlyList<string> AttachmentFilePaths, IReadOnlyList<string> ModelIds);
}
