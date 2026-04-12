using System;
using CleanSquad.Workflow;
using CleanSquad.Workflow.Orchestration;
using CleanSquad.Workflow.UnitTests.TestFixtures;

namespace CleanSquad.Workflow.UnitTests;

/// <summary>
///     Unit tests for <see cref="WorkflowRunState" />.
/// </summary>
public sealed class WorkflowRunStateTests
{
    /// <summary>
    ///     Verifies resume preparation re-queues interrupted work without duplicating existing pending activations.
    /// </summary>
    [Fact]
    public void PrepareForResumeRequeuesInterruptedWorkWithoutDuplicates()
    {
        FixedTimeProvider timeProvider = new(new DateTimeOffset(2026, 4, 11, 18, 0, 0, TimeSpan.Zero));
        WorkflowRunState state = WorkflowRunState.Create("run-1", "Test Workflow", "planner", timeProvider);
        state.Status = WorkflowRunStatus.Failed;
        state.CompletedAtUtc = timeProvider.GetUtcNow();
        state.PendingActivations.Clear();
        state.PendingActivations.Add(new WorkflowPendingActivation
        {
            SequenceNumber = 1,
            NodeId = "builder",
            ParallelGroupId = "group-1",
            BranchId = "code",
        });
        state.NextActivationSequenceNumber = 2;
        state.Steps.Add(new WorkflowStepState
        {
            StepNumber = 1,
            NodeId = "planner",
            Status = WorkflowStepStatus.InProgress,
            ActivationSequenceNumber = 2,
            StartedAtUtc = timeProvider.GetUtcNow(),
        });
        state.Steps.Add(new WorkflowStepState
        {
            StepNumber = 2,
            NodeId = "builder",
            Status = WorkflowStepStatus.Failed,
            ActivationSequenceNumber = 1,
            ParallelGroupId = "group-1",
            BranchId = "code",
            StartedAtUtc = timeProvider.GetUtcNow(),
        });

        state.PrepareForResume();

        Assert.Equal(WorkflowRunStatus.Running, state.Status);
        Assert.Null(state.CompletedAtUtc);
        Assert.Equal(2, state.PendingActivations.Count);
        Assert.Single(state.PendingActivations, activation => activation.NodeId == "builder");
        Assert.Contains(state.PendingActivations, activation => activation.NodeId == "planner");
        Assert.All(state.Steps, step => Assert.Equal(WorkflowStepStatus.Failed, step.Status));
    }

    /// <summary>
    ///     Verifies the latest completed output path is selected deterministically.
    /// </summary>
    [Fact]
    public void GetLatestOutputPathReturnsLatestCompletedStepOutput()
    {
        WorkflowRunState state = WorkflowRunState.Create("run-1", "Test Workflow", "planner", TimeProvider.System);
        state.Steps.Add(new WorkflowStepState
        {
            StepNumber = 1,
            NodeId = "builder",
            Status = WorkflowStepStatus.Completed,
            OutputPath = "first.md",
            StartedAtUtc = TimeProvider.System.GetUtcNow(),
            CompletedAtUtc = TimeProvider.System.GetUtcNow(),
        });
        state.Steps.Add(new WorkflowStepState
        {
            StepNumber = 2,
            NodeId = "builder",
            Status = WorkflowStepStatus.Failed,
            OutputPath = "failed.md",
            StartedAtUtc = TimeProvider.System.GetUtcNow(),
            CompletedAtUtc = TimeProvider.System.GetUtcNow(),
        });
        state.Steps.Add(new WorkflowStepState
        {
            StepNumber = 3,
            NodeId = "builder",
            Status = WorkflowStepStatus.Completed,
            OutputPath = "latest.md",
            StartedAtUtc = TimeProvider.System.GetUtcNow(),
            CompletedAtUtc = TimeProvider.System.GetUtcNow(),
        });

        string? outputPath = state.GetLatestOutputPath("builder");

        Assert.Equal("latest.md", outputPath);
    }
}
