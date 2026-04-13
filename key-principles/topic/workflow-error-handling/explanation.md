# Workflow Error Handling and Failure Delivery Reports

## What this document explains

This document describes how the CleanSquad workflow orchestrator handles failures and ensures that a delivery report is always produced, even when the workflow crashes. It covers the layered resilience strategy, the design rationale, and the four failure levels the system addresses.

## The problem

When an AI-driven workflow executes multiple agent stages in sequence, any stage can fail for reasons ranging from a malformed agent response to a complete Copilot SDK outage. Without error handling, a crash means no delivery report is produced, leaving the project team with no summary of what was attempted, what succeeded, and what went wrong.

This matters because the delivery report is the primary artefact that a CTO, CIO, or project manager reads to understand a workflow run. A missing report after a failure is worse than a partial report, because it provides no starting point for diagnosis or decision-making.

## The four levels of failure

The orchestrator recognises four distinct levels of failure, each requiring a different handling strategy.

### Level 1: Graceful stop

A decision node routes the workflow to a stop exit. This is not an error — it is a deliberate routing choice made by an agent. The workflow graph handles this by routing through a dedicated `delivery-report-stopped` stage node before reaching the `stopped` exit. The delivery reporter agent runs normally and produces a full narrative report explaining why the workflow was stopped and what evidence exists from the stages that did run.

This level requires no special error handling. It is handled entirely by the workflow graph definition.

### Level 2: Agent exception with SDK available

A specific agent stage throws an exception (timeout, malformed response, rate limit), but the Copilot SDK is still operational. The orchestrator catches the exception and attempts to invoke the configured `onErrorNodeId` agent node. Because the SDK is still alive, this produces a rich narrative delivery report that includes the agent's analysis of what went wrong.

### Level 3: SDK unavailable

The Copilot SDK itself is down or the agent runner cannot be reached. The `onErrorNodeId` agent invocation also fails. The orchestrator falls back to a mechanical delivery report generated entirely in C# from the persisted `WorkflowRunState`. This report includes a metrics summary table, an execution timeline, failure details, routing decisions, and residual risks. It is structured and informative, but lacks the narrative synthesis that an agent would provide.

### Level 4: Process crash

The host process crashes (out of memory, kill signal, power failure). No in-process error handling can run. The orchestrator persists `WorkflowRunState` to disk before every re-throw, so the state file survives. On resume, the `PrepareForResume()` method marks failed steps for retry, and the workflow can be restarted from the last known good state.

## The layered resilience strategy

The orchestrator implements a layered try-fallback pattern in the `catch (Exception)` block of `ExecuteAsync`:

1. **Set status to Failed** and persist the run state to disk. This ensures Level 4 recovery is always possible.
2. **Try Layer 1** — invoke the `onErrorNodeId` agent node. If the agent produces a delivery report, write it to both the step output path and the final markdown path. This is the best outcome after a failure because the agent can provide narrative context.
3. **Catch Layer 1 failure** — if the agent invocation also fails (SDK down, secondary timeout), log a warning and fall through.
4. **Try Layer 2** — generate a mechanical delivery report from `WorkflowRunState` using pure C#. No external dependencies. Write it to the final markdown path.
5. **Catch Layer 2 failure** — if even the mechanical report cannot be written (disk full, permissions), log the error. At this point, the persisted state file is still the last line of defence.
6. **Re-throw the original exception** — the caller always sees the original failure. The delivery report generation is a best-effort side effect, not a replacement for error propagation.

## How the on-error node works

The `onErrorNodeId` is an optional property on the `WorkflowDefinition` JSON. When set, it references a node in the workflow graph that the orchestrator invokes as a best-effort error handler. In the default workflow, this points to `delivery-report-stopped`, which uses the `delivery-reporter` agent.

The on-error invocation prepends a failure context block to the agent prompt, including:

- The exception type and message
- The workflow status at the time of failure
- Counts of completed and failed steps

This gives the agent enough context to write a meaningful failure analysis without requiring the full workflow graph to have routed to that node normally.

The on-error node is validated at load time — if the `onErrorNodeId` references a node that does not exist in the graph, the workflow definition fails validation.

## How the mechanical fallback report works

The mechanical report is generated by `BuildFailureDeliveryReport()` in the orchestrator. It reads only from `WorkflowRunState` and `WorkflowRunContext`, both of which are in-memory objects that were populated during the workflow run. The method:

- Counts completed steps, failed steps, decisions, review cycles, and rebuild passes
- Identifies the failure phase (the last failed step's node and role)
- Lists all steps with their status in execution order
- Lists all failed steps with their messages
- Lists all routing decisions with their outcomes
- Notes residual risks and recommends next steps

The report follows the same structured format as the agent-driven report (metrics table, timeline, risks) so that readers see a consistent shape regardless of which layer produced it.

## Design decisions and trade-offs

### Why not catch and retry the failed agent?

Retrying the failed agent is the job of the resume mechanism (Level 4). The on-error handler is specifically for producing a delivery report, not for retrying the work that failed. Mixing these concerns would make the error path unpredictable and harder to reason about.

### Why suppress CA1031 on the mechanical fallback?

The mechanical fallback `catch (Exception)` is intentionally broad because it is the absolute last resort. If the fallback itself throws, the only correct response is to log and continue with the original re-throw. Catching a more specific exception type would risk missing an unexpected failure mode in the report generation code, which would leave the run with no delivery report at all.

### Why prepend error context to the agent prompt instead of using a separate prompt?

The delivery reporter agent already has a well-defined prompt composition pipeline that includes shared assets, agent persona, agent rules, and input attachments. Replacing this with a separate prompt would lose all of that context. Prepending the error context gives the agent both the failure information and the full delivery context it needs to write a useful report.

### Why persist state before attempting the on-error node?

If the on-error node itself crashes the process (unlikely but possible), the persisted state ensures Level 4 recovery can still work. The sequence is always: persist state → attempt recovery → re-throw. This ordering guarantee means the state file is never stale when the process exits.

## Workflow JSON configuration

To enable the on-error handler, add the `onErrorNodeId` property to the workflow definition JSON at the root level:

```json
{
  "version": "2.3",
  "name": "Clean Agile Default Workflow",
  "defaultEntryPoint": "default",
  "onErrorNodeId": "delivery-report-stopped",
  "entryPoints": [ ... ],
  "nodes": [ ... ]
}
```

The referenced node must exist in the `nodes` array. It is typically a Stage node with the delivery reporter agent, but it can be any valid stage node.

## Telemetry

The error handling path emits structured log events and telemetry at each layer:

| Event ID | Event Name | Level | Meaning |
|----------|-----------|-------|---------|
| 1003 | workflow.run.failed | Error | The workflow failed with an unhandled exception |
| 1010 | workflow.onerror.agent.completed | Information | The on-error agent produced a delivery report |
| 1011 | workflow.onerror.agent.failed | Warning | The on-error agent failed, falling back to mechanical report |
| 1012 | workflow.onerror.mechanical.completed | Information | The mechanical fallback report was written |
| 1013 | workflow.onerror.mechanical.failed | Error | The mechanical fallback report could not be written |
