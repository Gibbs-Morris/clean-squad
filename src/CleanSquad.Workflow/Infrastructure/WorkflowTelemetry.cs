using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace CleanSquad.Workflow.Infrastructure;

/// <summary>
///     Exposes OpenTelemetry-compatible activities and metrics for workflow execution.
/// </summary>
public static class WorkflowTelemetry
{
    /// <summary>
    ///     Gets the activity source used for workflow tracing.
    /// </summary>
    public static ActivitySource ActivitySource { get; } = new("CleanSquad.Workflow");

    /// <summary>
    ///     Gets the meter used for workflow metrics.
    /// </summary>
    public static Meter Meter { get; } = new("CleanSquad.Workflow");

    /// <summary>
    ///     Gets the run counter.
    /// </summary>
    public static Counter<long> RunCounter { get; } = Meter.CreateCounter<long>("clean_squad.workflow.runs");

    /// <summary>
    ///     Gets the step counter.
    /// </summary>
    public static Counter<long> StepCounter { get; } = Meter.CreateCounter<long>("clean_squad.workflow.steps");

    /// <summary>
    ///     Gets the decision counter.
    /// </summary>
    public static Counter<long> DecisionCounter { get; } = Meter.CreateCounter<long>("clean_squad.workflow.decisions");
}
