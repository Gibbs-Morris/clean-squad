using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;
using CleanSquad.Workflow;
using CleanSquad.Workflow.Decisions;

namespace CleanSquad.Workflow.Orchestration;

/// <summary>
///     Persists deterministic workflow progress for resume, diagnostics, and final run reporting.
///     This state is serialized to disk so interrupted runs can be resumed safely.
/// </summary>
[JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
public sealed class WorkflowRunState
{
    /// <summary>
    ///     Gets or sets the schema version for persisted run state.
    /// </summary>
    public string SchemaVersion { get; set; } = "2.0";

    /// <summary>
    ///     Gets or sets the run identifier.
    /// </summary>
    public string RunId { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the workflow name.
    /// </summary>
    public string WorkflowName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the current run status.
    /// </summary>
    public WorkflowRunStatus Status { get; set; } = WorkflowRunStatus.Running;

    /// <summary>
    ///     Gets or sets the resolved entry node identifier.
    /// </summary>
    public string EntryNodeId { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the exit node identifier when the run has completed.
    /// </summary>
    public string? ExitNodeId { get; set; }

    /// <summary>
    ///     Gets or sets the UTC start timestamp.
    /// </summary>
    public DateTimeOffset StartedAtUtc { get; set; }

    /// <summary>
    ///     Gets or sets the UTC update timestamp.
    /// </summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }

    /// <summary>
    ///     Gets or sets the UTC completion timestamp.
    /// </summary>
    public DateTimeOffset? CompletedAtUtc { get; set; }

    /// <summary>
    ///     Gets or sets the next activation sequence number.
    /// </summary>
    public int NextActivationSequenceNumber { get; set; } = 1;

    /// <summary>
    ///     Gets or sets the next step number.
    /// </summary>
    public int NextStepNumber { get; set; } = 1;

    /// <summary>
    ///     Gets or sets the next parallel group sequence number.
    /// </summary>
    public int NextParallelGroupSequenceNumber { get; set; } = 1;

    /// <summary>
    ///     Gets the pending activations.
    /// </summary>
    public Collection<WorkflowPendingActivation> PendingActivations { get; } = [];

    /// <summary>
    ///     Gets the step history.
    /// </summary>
    public Collection<WorkflowStepState> Steps { get; } = [];

    /// <summary>
    ///     Gets the parallel group states.
    /// </summary>
    public Collection<WorkflowParallelGroupState> ParallelGroups { get; } = [];

    /// <summary>
    ///     Gets the decisions recorded during the run.
    /// </summary>
    public Collection<WorkflowDecision> Decisions { get; } = [];

    /// <summary>
    ///     Gets the node attempt counts.
    /// </summary>
    public Dictionary<string, int> NodeVisitCounts { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Creates a new persisted state for a workflow run.
    /// </summary>
    /// <param name="runId">The deterministic run identifier.</param>
    /// <param name="workflowName">The workflow display name captured for reporting.</param>
    /// <param name="entryNodeId">The first node identifier to enqueue for execution.</param>
    /// <param name="timeProvider">The time provider used to capture deterministic timestamps.</param>
    /// <returns>A new initialized workflow state ready for execution.</returns>
    public static WorkflowRunState Create(string runId, string workflowName, string entryNodeId, TimeProvider timeProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowName);
        ArgumentException.ThrowIfNullOrWhiteSpace(entryNodeId);
        ArgumentNullException.ThrowIfNull(timeProvider);

        DateTimeOffset now = timeProvider.GetUtcNow();
        WorkflowRunState state = new()
        {
            RunId = runId,
            WorkflowName = workflowName,
            EntryNodeId = entryNodeId,
            StartedAtUtc = now,
            UpdatedAtUtc = now,
        };
        state.Enqueue(entryNodeId);
        return state;
    }

    /// <summary>
    ///     Enqueues a new pending activation.
    /// </summary>
    /// <param name="nodeId">The target node identifier.</param>
    /// <param name="parallelGroupId">The optional parallel group identifier for forked branches.</param>
    /// <param name="branchId">The optional branch identifier for forked branches.</param>
    public void Enqueue(string nodeId, string? parallelGroupId = null, string? branchId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);

        this.PendingActivations.Add(new WorkflowPendingActivation
        {
            SequenceNumber = this.NextActivationSequenceNumber++,
            NodeId = nodeId,
            ParallelGroupId = parallelGroupId,
            BranchId = branchId,
        });
    }

    /// <summary>
    ///     Marks any in-progress work as pending so the run can resume safely.
    /// </summary>
    public void PrepareForResume()
    {
        foreach (WorkflowStepState step in this.Steps
                     .Where(step => step.Status is WorkflowStepStatus.InProgress or WorkflowStepStatus.Failed)
                     .OrderBy(step => step.ActivationSequenceNumber))
        {
            bool alreadyQueued = this.PendingActivations.Any(activation =>
                string.Equals(activation.NodeId, step.NodeId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(activation.ParallelGroupId, step.ParallelGroupId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(activation.BranchId, step.BranchId, StringComparison.OrdinalIgnoreCase));
            if (!alreadyQueued)
            {
                this.PendingActivations.Add(new WorkflowPendingActivation
                {
                    SequenceNumber = this.NextActivationSequenceNumber++,
                    NodeId = step.NodeId,
                    ParallelGroupId = step.ParallelGroupId,
                    BranchId = step.BranchId,
                });
            }

            step.Status = WorkflowStepStatus.Failed;
            step.Message = "Recovered for resume after an interrupted or failed execution.";
            step.CompletedAtUtc ??= step.StartedAtUtc;
        }

        WorkflowPendingActivation[] orderedActivations = this.PendingActivations
            .OrderBy(activation => activation.SequenceNumber)
            .ToArray();
        this.PendingActivations.Clear();
        foreach (WorkflowPendingActivation activation in orderedActivations)
        {
            this.PendingActivations.Add(activation);
        }

        if (this.Status is WorkflowRunStatus.Failed or WorkflowRunStatus.Stopped)
        {
            this.Status = WorkflowRunStatus.Running;
            this.CompletedAtUtc = null;
        }
    }

    /// <summary>
    ///     Gets the next node visit number.
    /// </summary>
    /// <param name="nodeId">The node identifier being visited.</param>
    /// <returns>The updated visit count for the node.</returns>
    public int IncrementNodeVisit(string nodeId)
    {
        if (!this.NodeVisitCounts.TryGetValue(nodeId, out int currentCount))
        {
            currentCount = 0;
        }

        currentCount++;
        this.NodeVisitCounts[nodeId] = currentCount;
        return currentCount;
    }

    /// <summary>
    ///     Gets the latest completed output path for a node.
    /// </summary>
    /// <param name="nodeId">The node identifier whose latest completed output should be returned.</param>
    /// <returns>
    ///     The latest completed output path for the node, or <see langword="null" /> when no completed output exists.
    /// </returns>
    public string? GetLatestOutputPath(string nodeId)
    {
        return this.Steps
            .Where(step => step.Status == WorkflowStepStatus.Completed && string.Equals(step.NodeId, nodeId, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(step.OutputPath))
            .OrderByDescending(step => step.StepNumber)
            .Select(step => step.OutputPath)
            .FirstOrDefault();
    }
}
