using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CleanSquad.Workflow;
using CleanSquad.Workflow.Decisions;
using CleanSquad.Workflow.Definitions;
using CleanSquad.Workflow.Infrastructure;
using CleanSquad.Workflow.Prompting;
using CleanSquad.Workflow.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CleanSquad.Workflow.Orchestration;

/// <summary>
///     Coordinates execution of a graph-defined workflow with persisted state, logs, and resume support.
/// </summary>
public sealed partial class WorkflowOrchestrator : IWorkflowOrchestrator
{
    private readonly IWorkflowArtifactService workflowArtifactService;
    private readonly IWorkflowAgentRunner workflowAgentRunner;
    private readonly IWorkflowDecisionResolver workflowDecisionResolver;
    private readonly ILogger<WorkflowOrchestrator> logger;
    private readonly TimeProvider timeProvider;

    /// <summary>
    ///     Initializes a new instance of the <see cref="WorkflowOrchestrator" /> class.
    /// </summary>
    /// <param name="workflowArtifactService">The workflow artifact service.</param>
    /// <param name="workflowAgentRunner">The workflow agent runner.</param>
    /// <param name="workflowDecisionResolver">The workflow decision resolver.</param>
    /// <param name="logger">The optional logger.</param>
    /// <param name="timeProvider">The optional time provider used for deterministic timestamps.</param>
    public WorkflowOrchestrator(
        IWorkflowArtifactService workflowArtifactService,
        IWorkflowAgentRunner workflowAgentRunner,
        IWorkflowDecisionResolver workflowDecisionResolver,
        ILogger<WorkflowOrchestrator>? logger = null,
        TimeProvider? timeProvider = null)
    {
        this.workflowArtifactService = workflowArtifactService ?? throw new ArgumentNullException(nameof(workflowArtifactService));
        this.workflowAgentRunner = workflowAgentRunner ?? throw new ArgumentNullException(nameof(workflowAgentRunner));
        this.workflowDecisionResolver = workflowDecisionResolver ?? throw new ArgumentNullException(nameof(workflowDecisionResolver));
        this.logger = logger ?? NullLogger<WorkflowOrchestrator>.Instance;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public Task<WorkflowRunResult> ExecuteAsync(
        string workspaceRootPath,
        string workflowDefinitionPath,
        string requestDocumentPath,
        CancellationToken cancellationToken = default)
    {
        return this.ExecuteAsync(
            new WorkflowExecutionRequest(
                workspaceRootPath,
                workflowDefinitionPath,
                requestDocumentPath),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowRunResult> ExecuteAsync(WorkflowExecutionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        WorkflowRunContext runContext = OpenRun(request);
        using Activity? runActivity = WorkflowTelemetry.ActivitySource.StartActivity("workflow.run");
        runActivity?.SetTag("workflow.name", runContext.Definition.Name);
        runActivity?.SetTag("workflow.run_id", runContext.Artifacts.RunId);
        WorkflowTelemetry.RunCounter.Add(1);
        Log(runContext.Artifacts, LogLevel.Information, 1000, "workflow.run.started", null, $"Workflow run '{runContext.Artifacts.RunId}' started.");
        this.LogWorkflowRunStarting(runContext.Definition.Name, runContext.Artifacts.RunId);
        this.workflowArtifactService.WriteState(runContext.Artifacts, runContext.State);

        try
        {
            while (runContext.State.Status == WorkflowRunStatus.Running)
            {
                cancellationToken.ThrowIfCancellationRequested();
                DrainControlActivations(runContext);
                if (runContext.State.Status != WorkflowRunStatus.Running)
                {
                    break;
                }

                List<WorkflowPendingActivation> stageWave = GetStageWave(runContext);
                if (stageWave.Count > 0)
                {
                    await ExecuteStageWaveAsync(runContext, stageWave, cancellationToken);
                    continue;
                }

                if (runContext.State.PendingActivations.Count == 0)
                {
                    throw new InvalidOperationException("The workflow has no pending activations but did not reach an exit node.");
                }

                WorkflowPendingActivation activation = runContext.State.PendingActivations[0];
                WorkflowNodeDefinition node = GetNode(runContext.Definition, activation.NodeId);
                if (node.Kind != WorkflowNodeKind.Decision)
                {
                    throw new InvalidOperationException($"The workflow cannot execute pending node '{node.Id}' of kind '{node.Kind}'.");
                }

                await ExecuteDecisionAsync(runContext, activation, cancellationToken);
            }

            string finalMarkdown = BuildFinalMarkdown(runContext.Definition, runContext.Artifacts, runContext.State);
            this.workflowArtifactService.WriteMarkdown(runContext.Artifacts.FinalMarkdownPath, finalMarkdown);
            this.workflowArtifactService.WriteState(runContext.Artifacts, runContext.State);
            runActivity?.SetTag("workflow.status", runContext.State.Status.ToString());
            Log(runContext.Artifacts, LogLevel.Information, 1001, "workflow.run.completed", runContext.State.ExitNodeId, $"Workflow run '{runContext.Artifacts.RunId}' completed with status '{runContext.State.Status}'.");
            this.LogWorkflowRunCompleted(runContext.Definition.Name, runContext.Artifacts.RunId, runContext.State.Status.ToString());

            return CreateResult(runContext);
        }
        catch (OperationCanceledException)
        {
            runContext.State.UpdatedAtUtc = this.timeProvider.GetUtcNow();
            this.workflowArtifactService.WriteState(runContext.Artifacts, runContext.State);
            Log(runContext.Artifacts, LogLevel.Warning, 1002, "workflow.run.cancelled", null, $"Workflow run '{runContext.Artifacts.RunId}' was cancelled and can be resumed.");
            throw;
        }
        catch (Exception exception)
        {
            runContext.State.Status = WorkflowRunStatus.Failed;
            runContext.State.UpdatedAtUtc = this.timeProvider.GetUtcNow();
            this.workflowArtifactService.WriteState(runContext.Artifacts, runContext.State);
            runActivity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            Log(runContext.Artifacts, LogLevel.Error, 1003, "workflow.run.failed", null, exception.Message, exception);
            throw;
        }
    }

    private WorkflowRunContext OpenRun(WorkflowExecutionRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.ResumePath))
        {
            WorkflowArtifacts artifacts = this.workflowArtifactService.LoadRunArtifacts(request.ResumePath);
            WorkflowDefinition definition = WorkflowDefinitionLoader.LoadFromFile(artifacts.WorkflowDefinitionPath);
            WorkflowRunState state = this.workflowArtifactService.ReadState(artifacts);
            state.PrepareForResume(this.timeProvider);

            if (!string.IsNullOrWhiteSpace(request.EntryPointId))
            {
                string overrideEntryNodeId = ResolveEntryNodeId(definition, request.EntryPointId);
                state.Enqueue(overrideEntryNodeId);
                state.EntryNodeId = overrideEntryNodeId;
                state.Status = WorkflowRunStatus.Running;
                state.CompletedAtUtc = null;
                state.WaitingNodes.Clear();
            }

            state.UpdatedAtUtc = this.timeProvider.GetUtcNow();
            return new WorkflowRunContext(definition, artifacts, state);
        }

        if (string.IsNullOrWhiteSpace(request.WorkflowDefinitionPath))
        {
            throw new InvalidOperationException("A workflow definition path is required for a new workflow run.");
        }

        if (string.IsNullOrWhiteSpace(request.RequestDocumentPath))
        {
            throw new InvalidOperationException("A markdown request path is required for a new workflow run.");
        }

        WorkflowDefinition newDefinition = WorkflowDefinitionLoader.LoadFromFile(request.WorkflowDefinitionPath);
        WorkflowArtifacts newArtifacts = this.workflowArtifactService.CreateRunArtifacts(
            request.WorkspaceRootPath,
            request.WorkflowDefinitionPath,
            request.RequestDocumentPath);
        WorkflowDefinitionLoader.SaveToFile(newDefinition, newArtifacts.WorkflowDefinitionPath);
        string entryNodeId = ResolveEntryNodeId(newDefinition, request.EntryPointId);
        WorkflowRunState newState = WorkflowRunState.Create(newArtifacts.RunId, newDefinition.Name, entryNodeId, this.timeProvider);
        return new WorkflowRunContext(newDefinition, newArtifacts, newState);
    }

    private void DrainControlActivations(WorkflowRunContext runContext)
    {
        while (runContext.State.Status == WorkflowRunStatus.Running && runContext.State.PendingActivations.Count > 0)
        {
            WorkflowPendingActivation activation = runContext.State.PendingActivations[0];
            WorkflowNodeDefinition node = GetNode(runContext.Definition, activation.NodeId);
            if (node.Kind is WorkflowNodeKind.Stage or WorkflowNodeKind.Decision)
            {
                return;
            }

            runContext.State.PendingActivations.RemoveAt(0);
            switch (node.Kind)
            {
                case WorkflowNodeKind.Fork:
                    ProcessFork(runContext, activation, node);
                    break;
                case WorkflowNodeKind.Join:
                    ProcessJoin(runContext, activation, node);
                    break;

                case WorkflowNodeKind.Wait:
                    ProcessWait(runContext, activation, node);
                    break;

                case WorkflowNodeKind.Exit:
                    ProcessExit(runContext, activation, node);
                    break;
            }
        }
    }

    private static List<WorkflowPendingActivation> GetStageWave(WorkflowRunContext runContext)
    {
        List<WorkflowPendingActivation> wave = [];
        foreach (WorkflowPendingActivation activation in runContext.State.PendingActivations.OrderBy(activation => activation.SequenceNumber))
        {
            WorkflowNodeDefinition node = GetNode(runContext.Definition, activation.NodeId);
            if (node.Kind != WorkflowNodeKind.Stage)
            {
                if (wave.Count > 0)
                {
                    break;
                }

                continue;
            }

            wave.Add(activation);
            if (wave.Count >= runContext.Definition.Policy.MaxParallelism)
            {
                break;
            }
        }

        return wave;
    }

    private async Task ExecuteStageWaveAsync(
        WorkflowRunContext runContext,
        IReadOnlyList<WorkflowPendingActivation> activations,
        CancellationToken cancellationToken)
    {
        List<PreparedStageExecution> preparedExecutions = [];
        foreach (WorkflowPendingActivation activation in activations.OrderBy(activation => activation.SequenceNumber))
        {
            runContext.State.PendingActivations.Remove(activation);
            WorkflowNodeDefinition node = GetNode(runContext.Definition, activation.NodeId);
            WorkflowStepState step = StartStep(runContext, activation, node);
            IReadOnlyList<string> attachments = ResolveAttachmentFilePaths(runContext, node);
            preparedExecutions.Add(new PreparedStageExecution(activation, node, step, attachments));
            Log(runContext.Artifacts, LogLevel.Information, 2000, "workflow.step.started", node.Id, $"Started node '{node.Id}' step {step.StepNumber:0000}.");
        }

        this.workflowArtifactService.WriteState(runContext.Artifacts, runContext.State);

        StageExecutionOutcome[] outcomes = await Task.WhenAll(preparedExecutions.Select(execution => ExecuteStageAsync(runContext.Definition, execution, cancellationToken)));
        Exception? firstFailure = null;
        foreach (StageExecutionOutcome outcome in outcomes.OrderBy(outcome => outcome.Step.StepNumber))
        {
            if (outcome.Exception is null)
            {
                CompleteStageStep(runContext, outcome);
                continue;
            }

            FailStep(runContext, outcome.Step, outcome.Exception.Message);
            firstFailure ??= outcome.Exception;
        }

        this.workflowArtifactService.WriteState(runContext.Artifacts, runContext.State);
        if (firstFailure is not null)
        {
            throw new InvalidOperationException(firstFailure.Message, firstFailure);
        }
    }

    private async Task ExecuteDecisionAsync(
        WorkflowRunContext runContext,
        WorkflowPendingActivation activation,
        CancellationToken cancellationToken)
    {
        runContext.State.PendingActivations.RemoveAt(0);
        WorkflowNodeDefinition node = GetNode(runContext.Definition, activation.NodeId);
        WorkflowStepState step = StartStep(runContext, activation, node);
        List<string> attachments = ResolveAttachmentFilePaths(runContext, node);
        string sourceMarkdown = ResolveSourceMarkdown(runContext, node, attachments);
        this.workflowArtifactService.WriteState(runContext.Artifacts, runContext.State);
        Log(runContext.Artifacts, LogLevel.Information, 3000, "workflow.decision.started", node.Id, $"Started decision node '{node.Id}'.");

        this.LogDecisionEvaluating(node.Id, step.StepNumber, attachments.Count);
        try
        {
            WorkflowDecision decision = await this.workflowDecisionResolver.ResolveAsync(
                new WorkflowDecisionContext(
                    runContext.Definition,
                    node,
                    runContext.Artifacts,
                    runContext.State,
                    attachments,
                    sourceMarkdown),
                cancellationToken);
            string decisionMarkdown = BuildDecisionMarkdown(node, decision);
            this.workflowArtifactService.WriteMarkdown(step.OutputPath!, decisionMarkdown);
            step.Status = WorkflowStepStatus.Completed;
            step.CompletedAtUtc = this.timeProvider.GetUtcNow();
            step.Message = decision.Reason;
            step.ChoiceId = decision.ChoiceId;
            step.NextNodeId = decision.NextNodeId;
            runContext.State.Decisions.Add(decision);
            WorkflowTelemetry.DecisionCounter.Add(1);

            if (!string.IsNullOrWhiteSpace(decision.NextNodeId))
            {
                runContext.State.Enqueue(decision.NextNodeId, activation.ParallelGroupId, activation.BranchId);
            }

            runContext.State.UpdatedAtUtc = this.timeProvider.GetUtcNow();
            this.workflowArtifactService.WriteState(runContext.Artifacts, runContext.State);
            Log(runContext.Artifacts, LogLevel.Information, 3001, "workflow.decision.completed", node.Id, $"Decision node '{node.Id}' selected '{decision.ChoiceId ?? decision.Action.ToString()}'.");
            this.LogDecisionResolved(
                node.Id,
                step.StepNumber,
                decision.ChoiceId ?? decision.Action.ToString(),
                decision.NextNodeId ?? "(none)");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            FailStep(runContext, step, exception.Message);
            this.workflowArtifactService.WriteState(runContext.Artifacts, runContext.State);
            this.LogDecisionNodeFailed(exception, node.Id, runContext.Artifacts.RunId);
            throw new InvalidOperationException(exception.Message, exception);
        }
    }

    private void ProcessFork(WorkflowRunContext runContext, WorkflowPendingActivation activation, WorkflowNodeDefinition node)
    {
        CreateCompletedControlStep(runContext, activation, node, $"Forked {node.Branches.Count} branches.");
        string groupId = $"{node.Id}-{runContext.State.NextParallelGroupSequenceNumber:0000}";
        runContext.State.NextParallelGroupSequenceNumber++;
        runContext.State.ParallelGroups.Add(new WorkflowParallelGroupState
        {
            GroupId = groupId,
            ForkNodeId = node.Id,
            JoinNodeId = node.JoinNodeId ?? throw new InvalidOperationException($"Fork node '{node.Id}' must define joinNodeId."),
        });
        WorkflowParallelGroupState parallelGroup = runContext.State.ParallelGroups[^1];
        foreach (string branchId in node.Branches.Select(branch => branch.Id))
        {
            parallelGroup.ExpectedBranchIds.Add(branchId);
        }

        foreach (WorkflowForkBranchDefinition branch in node.Branches.OrderBy(branch => branch.Id, StringComparer.OrdinalIgnoreCase))
        {
            runContext.State.Enqueue(branch.NextNodeId, groupId, branch.Id);
        }

        runContext.State.UpdatedAtUtc = this.timeProvider.GetUtcNow();
        this.workflowArtifactService.WriteState(runContext.Artifacts, runContext.State);
        Log(runContext.Artifacts, LogLevel.Information, 4000, "workflow.fork.completed", node.Id, $"Fork node '{node.Id}' created group '{groupId}'.");
        this.LogForkProcessed(node.Id, node.Branches.Count);
    }

    private void ProcessJoin(WorkflowRunContext runContext, WorkflowPendingActivation activation, WorkflowNodeDefinition node)
    {
        if (string.IsNullOrWhiteSpace(activation.ParallelGroupId))
        {
            throw new InvalidOperationException($"Join node '{node.Id}' requires a parallel group id.");
        }

        if (string.IsNullOrWhiteSpace(activation.BranchId))
        {
            throw new InvalidOperationException($"Join node '{node.Id}' requires a branch id.");
        }

        WorkflowParallelGroupState group = runContext.State.ParallelGroups.FirstOrDefault(group => string.Equals(group.GroupId, activation.ParallelGroupId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"The parallel group '{activation.ParallelGroupId}' could not be found for join node '{node.Id}'.");
        if (!group.CompletedBranchIds.Contains(activation.BranchId, StringComparer.OrdinalIgnoreCase))
        {
            group.CompletedBranchIds.Add(activation.BranchId);
        }

        if (group.CompletedBranchIds.Count < group.ExpectedBranchIds.Count || group.JoinReleased)
        {
            runContext.State.UpdatedAtUtc = this.timeProvider.GetUtcNow();
            this.workflowArtifactService.WriteState(runContext.Artifacts, runContext.State);
            Log(runContext.Artifacts, LogLevel.Information, 4001, "workflow.join.waiting", node.Id, $"Join node '{node.Id}' is waiting for remaining branches.");
            this.LogJoinProgress(node.Id, group.CompletedBranchIds.Count, group.ExpectedBranchIds.Count);
            return;
        }

        group.JoinReleased = true;
        WorkflowStepState step = CreateCompletedControlStep(runContext, activation, node, $"Joined {group.CompletedBranchIds.Count} branches.");
        if (!string.IsNullOrWhiteSpace(node.Next))
        {
            step.NextNodeId = node.Next;
            runContext.State.Enqueue(node.Next);
        }

        runContext.State.UpdatedAtUtc = this.timeProvider.GetUtcNow();
        this.workflowArtifactService.WriteState(runContext.Artifacts, runContext.State);
        Log(runContext.Artifacts, LogLevel.Information, 4002, "workflow.join.completed", node.Id, $"Join node '{node.Id}' released group '{group.GroupId}'.");
        this.LogJoinReleased(node.Id, group.CompletedBranchIds.Count);
    }

    private void ProcessExit(WorkflowRunContext runContext, WorkflowPendingActivation activation, WorkflowNodeDefinition node)
    {
        CreateCompletedControlStep(runContext, activation, node, $"Exited workflow with status '{node.ExitStatus}'.");
        runContext.State.Status = node.ExitStatus;
        runContext.State.ExitNodeId = node.Id;
        runContext.State.CompletedAtUtc = this.timeProvider.GetUtcNow();
        runContext.State.UpdatedAtUtc = runContext.State.CompletedAtUtc.Value;
        this.workflowArtifactService.WriteState(runContext.Artifacts, runContext.State);
        Log(runContext.Artifacts, LogLevel.Information, 4003, "workflow.exit.reached", node.Id, $"Exit node '{node.Id}' set status '{node.ExitStatus}'.");
        this.LogExitReached(node.Id, node.ExitStatus.ToString());
    }

    private void ProcessWait(WorkflowRunContext runContext, WorkflowPendingActivation activation, WorkflowNodeDefinition node)
    {
        TimeSpan waitDuration = ParseWaitDuration(node);
        DateTimeOffset waitStartedAtUtc = this.timeProvider.GetUtcNow();
        DateTimeOffset waitUntilUtc = waitStartedAtUtc.Add(waitDuration);
        string nextNodeId = node.Next ?? throw new InvalidOperationException($"Wait node '{node.Id}' must define next.");
        string waitReason = string.IsNullOrWhiteSpace(node.WaitReason)
            ? $"Paused the workflow for {node.WaitDuration} before continuing to '{nextNodeId}'."
            : node.WaitReason;
        string message = $"{waitReason} Resume after {waitUntilUtc:O}.";

        WorkflowStepState step = CreateCompletedControlStep(runContext, activation, node, message);
        step.NextNodeId = nextNodeId;
        runContext.State.WaitingNodes.Add(new WorkflowWaitState
        {
            NodeId = node.Id,
            NextNodeId = nextNodeId,
            ParallelGroupId = activation.ParallelGroupId,
            BranchId = activation.BranchId,
            WaitDuration = node.WaitDuration!,
            Reason = waitReason,
            WaitStartedAtUtc = waitStartedAtUtc,
            WaitUntilUtc = waitUntilUtc,
        });
        runContext.State.Status = WorkflowRunStatus.Paused;
        runContext.State.UpdatedAtUtc = waitStartedAtUtc;
        this.workflowArtifactService.WriteState(runContext.Artifacts, runContext.State);
        Log(runContext.Artifacts, LogLevel.Information, 4004, "workflow.wait.started", node.Id, $"Wait node '{node.Id}' paused the workflow until {waitUntilUtc:O}.");
        this.LogWaitStarted(node.Id, waitUntilUtc.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
    }

    private WorkflowStepState StartStep(WorkflowRunContext runContext, WorkflowPendingActivation activation, WorkflowNodeDefinition node)
    {
        int visitCount = runContext.State.IncrementNodeVisit(node.Id);
        if (visitCount > runContext.Definition.Policy.MaxNodeVisits)
        {
            throw new InvalidOperationException($"The workflow exceeded policy.maxNodeVisits for node '{node.Id}'.");
        }

        int stepNumber = runContext.State.NextStepNumber++;
        WorkflowStepState step = new()
        {
            StepNumber = stepNumber,
            NodeId = node.Id,
            NodeKind = node.Kind,
            RoleName = node.Role ?? node.Id,
            AgentName = ResolveAgentName(node),
            Models = node.Models,
            ReasoningEffort = WorkflowReasoningEffort.Normalize(node.ReasoningEffort),
            InputReferences = node.Inputs,
            OutputNames = node.Outputs,
            CustomMessage = node.CustomMessage,
            Attempt = visitCount,
            ActivationSequenceNumber = activation.SequenceNumber,
            OutputPath = runContext.Artifacts.GetStepMarkdownPath(stepNumber, node.Id),
            ParallelGroupId = activation.ParallelGroupId,
            BranchId = activation.BranchId,
            Status = WorkflowStepStatus.InProgress,
            StartedAtUtc = this.timeProvider.GetUtcNow(),
        };
        runContext.State.Steps.Add(step);
        runContext.State.UpdatedAtUtc = step.StartedAtUtc;
        WorkflowTelemetry.StepCounter.Add(1);
        return step;
    }

    private WorkflowStepState CreateCompletedControlStep(
        WorkflowRunContext runContext,
        WorkflowPendingActivation activation,
        WorkflowNodeDefinition node,
        string message)
    {
        WorkflowStepState step = StartStep(runContext, activation, node);
        step.Status = WorkflowStepStatus.Completed;
        step.CompletedAtUtc = this.timeProvider.GetUtcNow();
        step.Message = message;
        WriteControlStepMarkdown(step, node, message);
        return step;
    }

    /// <summary>
    ///     Writes a short summary markdown file for a control-flow node (Fork, Join, or Exit)
    ///     so that every step number in the run directory has a corresponding visible file.
    /// </summary>
    private void WriteControlStepMarkdown(WorkflowStepState step, WorkflowNodeDefinition node, string message)
    {
        if (string.IsNullOrWhiteSpace(step.OutputPath))
        {
            return;
        }

        string branchList = node.Branches.Count > 0
            ? string.Join("\n", node.Branches.Select(b => $"- **{b.Id}** → `{b.NextNodeId}`"))
            : string.Empty;

        string markdown = $"""
            # {node.DisplayName ?? node.Id}

            **Kind:** {node.Kind}
            **Status:** {step.Status}

            {message}
            """;

        if (!string.IsNullOrEmpty(branchList))
        {
            markdown += $"\n\n## Branches\n\n{branchList}\n";
        }

        this.workflowArtifactService.WriteMarkdown(step.OutputPath, markdown);
    }

    private void CompleteStageStep(WorkflowRunContext runContext, StageExecutionOutcome outcome)
    {
        outcome.Step.Status = WorkflowStepStatus.Completed;
        outcome.Step.CompletedAtUtc = this.timeProvider.GetUtcNow();
        outcome.Step.Message = $"Completed node '{outcome.Node.Id}'.";
        if (!string.IsNullOrWhiteSpace(outcome.Node.Next))
        {
            outcome.Step.NextNodeId = outcome.Node.Next;
            runContext.State.Enqueue(outcome.Node.Next, outcome.Activation.ParallelGroupId, outcome.Activation.BranchId);
        }

        runContext.State.UpdatedAtUtc = this.timeProvider.GetUtcNow();
        Log(runContext.Artifacts, LogLevel.Information, 2001, "workflow.step.completed", outcome.Node.Id, $"Completed node '{outcome.Node.Id}' step {outcome.Step.StepNumber:0000}.");
    }

    private void FailStep(WorkflowRunContext runContext, WorkflowStepState step, string message)
    {
        step.Status = WorkflowStepStatus.Failed;
        step.CompletedAtUtc = this.timeProvider.GetUtcNow();
        step.Message = message;
        runContext.State.UpdatedAtUtc = this.timeProvider.GetUtcNow();
        Log(runContext.Artifacts, LogLevel.Error, 2002, "workflow.step.failed", step.NodeId, message);
        this.LogAgentFailed(step.AgentName, step.NodeId, step.StepNumber);
    }

    private async Task<StageExecutionOutcome> ExecuteStageAsync(
        WorkflowDefinition definition,
        PreparedStageExecution execution,
        CancellationToken cancellationToken)
    {
        try
        {
            using Activity? activity = WorkflowTelemetry.ActivitySource.StartActivity("workflow.step");
            activity?.SetTag("workflow.node", execution.Node.Id);
            activity?.SetTag("workflow.role", execution.Node.Role ?? execution.Node.Id);
            activity?.SetTag("workflow.agent", ResolveAgentName(execution.Node));
            if (execution.Step.Models.Count > 0)
            {
                activity?.SetTag("workflow.models", string.Join(",", execution.Step.Models));
            }

            if (!string.IsNullOrWhiteSpace(execution.Step.ReasoningEffort))
            {
                activity?.SetTag("workflow.reasoning_effort", execution.Step.ReasoningEffort);
            }

            string prompt = await WorkflowPromptComposer.ComposeAsync(definition, execution.Node, execution.AttachmentFilePaths, cancellationToken);
            string agentName = ResolveAgentName(execution.Node);
            this.LogAgentInvoking(
                agentName,
                execution.Node.Id,
                execution.Step.StepNumber,
                execution.AttachmentFilePaths.Count,
                FormatAttachmentNames(execution.AttachmentFilePaths));
            string markdown = await this.workflowAgentRunner.RunAsync(
                agentName,
                prompt,
                execution.AttachmentFilePaths,
                execution.Step.Models,
                execution.Step.ReasoningEffort,
                cancellationToken);
            this.LogAgentFinished(agentName, execution.Node.Id, execution.Step.StepNumber, markdown.Length);
            this.workflowArtifactService.WriteMarkdown(execution.Step.OutputPath!, markdown);
            return new StageExecutionOutcome(execution.Activation, execution.Node, execution.Step, markdown, null);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new StageExecutionOutcome(execution.Activation, execution.Node, execution.Step, null, exception);
        }
    }

    private static List<string> ResolveAttachmentFilePaths(WorkflowRunContext runContext, WorkflowNodeDefinition node)
    {
        List<string> attachments = [];
        IReadOnlyList<string> inputs = node.Inputs.Count > 0 ? node.Inputs : ["request"];
        foreach (string input in inputs)
        {
            if (string.Equals(input, "request", StringComparison.OrdinalIgnoreCase))
            {
                attachments.Add(runContext.Artifacts.RequestMarkdownPath);
                continue;
            }

            if (string.Equals(input, "workflow", StringComparison.OrdinalIgnoreCase))
            {
                attachments.Add(runContext.Artifacts.WorkflowDefinitionPath);
                continue;
            }

            if (input.StartsWith("node:", StringComparison.OrdinalIgnoreCase))
            {
                string sourceNodeId = input[5..];
                string? outputPath = runContext.State.GetLatestOutputPath(sourceNodeId);
                if (!string.IsNullOrWhiteSpace(outputPath) && File.Exists(outputPath))
                {
                    attachments.Add(outputPath);
                }
            }
        }

        return attachments.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string ResolveSourceMarkdown(
        WorkflowRunContext runContext,
        WorkflowNodeDefinition node,
        IReadOnlyList<string> attachments)
    {
        if (!string.IsNullOrWhiteSpace(node.DecisionSourceNodeId))
        {
            string? sourcePath = runContext.State.GetLatestOutputPath(node.DecisionSourceNodeId);
            if (!string.IsNullOrWhiteSpace(sourcePath) && File.Exists(sourcePath))
            {
                return MarkdownArtifactService.ReadMarkdown(sourcePath);
            }
        }

        string? latestAttachment = attachments.LastOrDefault(path => File.Exists(path));
        return string.IsNullOrWhiteSpace(latestAttachment) ? string.Empty : MarkdownArtifactService.ReadMarkdown(latestAttachment);
    }

    private static WorkflowNodeDefinition GetNode(WorkflowDefinition definition, string nodeId)
    {
        return definition.Nodes.First(node => string.Equals(node.Id, nodeId, StringComparison.OrdinalIgnoreCase));
    }

    private static string ResolveEntryNodeId(WorkflowDefinition definition, string? entryPointId)
    {
        string resolvedEntryId = string.IsNullOrWhiteSpace(entryPointId)
            ? definition.DefaultEntryPoint ?? definition.EntryPoints[0].Id
            : entryPointId;
        WorkflowEntryPointDefinition? entryPoint = definition.EntryPoints.FirstOrDefault(entryPoint =>
            string.Equals(entryPoint.Id, resolvedEntryId, StringComparison.OrdinalIgnoreCase));
        if (entryPoint is not null)
        {
            return entryPoint.NodeId;
        }

        if (definition.Nodes.Any(node => string.Equals(node.Id, resolvedEntryId, StringComparison.OrdinalIgnoreCase)))
        {
            return resolvedEntryId;
        }

        throw new InvalidOperationException($"The workflow entry point '{resolvedEntryId}' is not defined.");
    }

    private static TimeSpan ParseWaitDuration(WorkflowNodeDefinition node)
    {
        if (string.IsNullOrWhiteSpace(node.WaitDuration)
            || !TimeSpan.TryParse(node.WaitDuration, System.Globalization.CultureInfo.InvariantCulture, out TimeSpan waitDuration)
            || waitDuration <= TimeSpan.Zero)
        {
            throw new InvalidOperationException($"Wait node '{node.Id}' must define a positive waitDuration.");
        }

        return waitDuration;
    }

    private static string BuildDecisionMarkdown(WorkflowNodeDefinition node, WorkflowDecision decision)
    {
        return $"""
# Decision
Node: {node.Id}
Choice: {decision.ChoiceId ?? decision.Action.ToString()}
Next: {decision.NextNodeId ?? "(none)"}
Source: {decision.Source}
Reason: {decision.Reason}

## Raw Content
{decision.RawContent.Trim()}
""";
    }

    private static string BuildFinalMarkdown(WorkflowDefinition definition, WorkflowArtifacts artifacts, WorkflowRunState state)
    {
        WorkflowStepState? lastCompletedOutput = state.Steps
            .Where(step => step.Status == WorkflowStepStatus.Completed && !string.IsNullOrWhiteSpace(step.OutputPath) && File.Exists(step.OutputPath))
            .OrderByDescending(step => step.StepNumber)
            .FirstOrDefault();
        string finalContent = lastCompletedOutput is null
            ? "No markdown artifact was produced."
            : MarkdownArtifactService.ReadMarkdown(lastCompletedOutput.OutputPath!);
        string decisionList = state.Decisions.Count == 0
            ? "- none"
            : string.Join(Environment.NewLine, state.Decisions.Select((decision, index) =>
                $"- {index + 1:00}: {decision.ChoiceId ?? decision.Action.ToString()} -> {decision.NextNodeId ?? "(none)"} ({decision.Source})"));
        string stepList = state.Steps.Count == 0
            ? "- none"
            : string.Join(Environment.NewLine, state.Steps.OrderBy(step => step.StepNumber).Select(step =>
                $"- {step.StepNumber:0000}: {step.NodeId} [{step.Status}]" + FormatExecutionSettingsSuffix(step.Models, step.ReasoningEffort)));

        return $"""
# Final Workflow Output
Workflow: {definition.Name}
Status: {state.Status}
EntryNode: {state.EntryNodeId}
ExitNode: {state.ExitNodeId ?? "(not reached)"}
RunId: {artifacts.RunId}

## Files
- workflow: {Path.GetFileName(artifacts.WorkflowDefinitionPath)}
- request: {Path.GetFileName(artifacts.RequestMarkdownPath)}
- final: {Path.GetFileName(artifacts.FinalMarkdownPath)}
- state-json: {Path.GetFileName(artifacts.StateJsonPath)}
- events: {Path.GetFileName(artifacts.EventLogPath)}

## Steps
{stepList}

## Decisions
{decisionList}

## Final Artifact Content
{finalContent.Trim()}
""";
    }

    private static WorkflowRunResult CreateResult(WorkflowRunContext runContext)
    {
        int rebuildCount = CountCompletedSteps(runContext.State, "Rebuilder", "rebuilder");
        int reviewCount = CountCompletedSteps(runContext.State, "Reviewer", "reviewer");
        return new WorkflowRunResult(
            runContext.Artifacts.RunDirectoryPath,
            runContext.Artifacts.FinalMarkdownPath,
            runContext.Artifacts.StateMarkdownPath,
            runContext.State.Status,
            runContext.State.Status == WorkflowRunStatus.Approved,
            rebuildCount,
            reviewCount,
            runContext.State.Decisions.ToArray());
    }

    private static int CountCompletedSteps(WorkflowRunState state, params string[] roleNames)
    {
        return state.Steps.Count(step =>
            step.Status == WorkflowStepStatus.Completed
            && roleNames.Any(roleName => string.Equals(step.RoleName, roleName, StringComparison.OrdinalIgnoreCase)));
    }

    private static string FormatExecutionSettingsSuffix(IReadOnlyList<string> models, string? reasoningEffort)
    {
        List<string> segments = [];
        if (models.Count > 0)
        {
            segments.Add($"models={string.Join(",", models)}");
        }

        if (!string.IsNullOrWhiteSpace(reasoningEffort))
        {
            segments.Add($"reasoningEffort={reasoningEffort}");
        }

        return segments.Count == 0 ? string.Empty : $" {string.Join(" ", segments)}";
    }

    private static string ResolveAgentName(WorkflowNodeDefinition node)
    {
        if (!string.IsNullOrWhiteSpace(node.Agent))
        {
            return node.Agent;
        }

        return string.IsNullOrWhiteSpace(node.Role) ? node.Id : node.Role;
    }

    private static string FormatAttachmentNames(IReadOnlyList<string> paths)
    {
        return paths.Count == 0 ? "(none)" : string.Join(", ", paths.Select(Path.GetFileName));
    }

    private void Log(
        WorkflowArtifacts artifacts,
        LogLevel level,
        int eventId,
        string eventName,
        string? nodeId,
        string message,
        Exception? exception = null)
    {
        Dictionary<string, string> properties = new(StringComparer.OrdinalIgnoreCase)
        {
            ["runId"] = artifacts.RunId,
            ["eventId"] = eventId.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };
        if (!string.IsNullOrWhiteSpace(nodeId))
        {
            properties["nodeId"] = nodeId;
        }

        if (exception is not null)
        {
            properties["exceptionType"] = exception.GetType().FullName ?? exception.GetType().Name;
            properties["exceptionMessage"] = exception.Message;
        }

        WorkflowLogEntry entry = new()
        {
            TimestampUtc = this.timeProvider.GetUtcNow(),
            Level = level.ToString(),
            EventName = eventName,
            NodeId = nodeId,
            Message = message,
        };
        foreach (KeyValuePair<string, string> property in properties)
        {
            entry.Properties[property.Key] = property.Value;
        }

        this.workflowArtifactService.AppendLog(artifacts, entry);
    }

    private sealed record WorkflowRunContext(WorkflowDefinition Definition, WorkflowArtifacts Artifacts, WorkflowRunState State);

    private sealed record PreparedStageExecution(
        WorkflowPendingActivation Activation,
        WorkflowNodeDefinition Node,
        WorkflowStepState Step,
        IReadOnlyList<string> AttachmentFilePaths);

    private sealed record StageExecutionOutcome(
        WorkflowPendingActivation Activation,
        WorkflowNodeDefinition Node,
        WorkflowStepState Step,
        string? Markdown,
        Exception? Exception);

    [LoggerMessage(EventId = 400, Level = LogLevel.Error, Message = "Decision node {NodeId} failed for run {RunId}.")]
    private partial void LogDecisionNodeFailed(Exception exception, string nodeId, string runId);

    [LoggerMessage(EventId = 401, Level = LogLevel.Information, Message = "Workflow '{WorkflowName}' (run {RunId}) starting.")]
    private partial void LogWorkflowRunStarting(string workflowName, string runId);

    [LoggerMessage(EventId = 402, Level = LogLevel.Information, Message = "Workflow '{WorkflowName}' (run {RunId}) completed with status '{Status}'.")]
    private partial void LogWorkflowRunCompleted(string workflowName, string runId, string status);

    [LoggerMessage(EventId = 410, Level = LogLevel.Information, Message = "Invoking agent '{AgentName}' for node '{NodeId}' (step {StepNumber}) with {AttachmentCount} file(s): {AttachmentNames}")]
    private partial void LogAgentInvoking(string agentName, string nodeId, int stepNumber, int attachmentCount, string attachmentNames);

    [LoggerMessage(EventId = 411, Level = LogLevel.Information, Message = "Agent '{AgentName}' for node '{NodeId}' (step {StepNumber}) finished ({ResponseLength} chars).")]
    private partial void LogAgentFinished(string agentName, string nodeId, int stepNumber, int responseLength);

    [LoggerMessage(EventId = 412, Level = LogLevel.Error, Message = "Agent '{AgentName}' for node '{NodeId}' (step {StepNumber}) failed.")]
    private partial void LogAgentFailed(string agentName, string nodeId, int stepNumber);

    [LoggerMessage(EventId = 413, Level = LogLevel.Information, Message = "Evaluating decision '{NodeId}' (step {StepNumber}) from {AttachmentCount} source(s).")]
    private partial void LogDecisionEvaluating(string nodeId, int stepNumber, int attachmentCount);

    [LoggerMessage(EventId = 414, Level = LogLevel.Information, Message = "Decision '{NodeId}' (step {StepNumber}): choice='{Choice}', next='{NextNode}'.")]
    private partial void LogDecisionResolved(string nodeId, int stepNumber, string choice, string nextNode);

    [LoggerMessage(EventId = 420, Level = LogLevel.Information, Message = "Fork '{NodeId}' started {BranchCount} branch(es).")]
    private partial void LogForkProcessed(string nodeId, int branchCount);

    [LoggerMessage(EventId = 421, Level = LogLevel.Information, Message = "Join '{NodeId}': {CompletedCount}/{ExpectedCount} branch(es) completed, waiting for more.")]
    private partial void LogJoinProgress(string nodeId, int completedCount, int expectedCount);

    [LoggerMessage(EventId = 422, Level = LogLevel.Information, Message = "Join '{NodeId}' released all {CompletedCount} branch(es).")]
    private partial void LogJoinReleased(string nodeId, int completedCount);

    [LoggerMessage(EventId = 423, Level = LogLevel.Information, Message = "Exit '{NodeId}' reached with status '{ExitStatus}'.")]
    private partial void LogExitReached(string nodeId, string exitStatus);

    [LoggerMessage(EventId = 424, Level = LogLevel.Information, Message = "Wait '{NodeId}' paused the workflow until '{ResumeAfterUtc}'.")]
    private partial void LogWaitStarted(string nodeId, string resumeAfterUtc);
}
