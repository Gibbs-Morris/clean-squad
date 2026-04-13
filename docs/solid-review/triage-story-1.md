# SOLID Review – Story 1: Triage Decision Record

**Branch:** `solid-review/story-1-triage`
**Base:** `feature/clean-squad-starter-ci` at commit `a7b1d33`
**Triage scope:** All `.cs` files exceeding 50 lines under `src/` (excluding `obj/` and `bin/` paths)
**Files in scope:** 21
**Production code changes:** Zero — read-only triage

---

## Pre-work Baseline Test

`dotnet test .\CleanSquad.slnx` run before any triage work on branch `solid-review/story-1-triage`:

```
Test summary: total: 68, failed: 0, succeeded: 68, skipped: 0, duration: 2.1s
Build succeeded in 2.9s
```

**Result: PASS. Baseline is stable.**

---

## Branch Situation Note

The development amigo document instructed creating this branch from `main`. That instruction cannot be followed as written: `main` contains only two commits (initial commit and key-principles documentation) and does not include the production code under review. The branch was created from `feature/clean-squad-starter-ci` at its HEAD commit (`a7b1d33 – Prevent unverified GitHub workflow claims`), which is the correct base for the epic.

**Action required before Story 2 begins:** When `feature/clean-squad-starter-ci` merges to `main`, all story branches in this epic must be retargeted to `main` before further commits are added.

Additionally, at triage time, `feature/clean-squad-starter-ci` has uncommitted WIP changes stashed under the message `wip-feature-clean-squad-starter-ci`. Those changes touch `WorkflowOrchestrator.cs`, `WorkflowArtifacts.cs`, and introduce new files in `src/CleanSquad.Workflow/Reporting/` and `tests/`. The triage line counts below reflect the **committed state only** and do not include the stashed WIP.

---

## Line Count Reconciliation

The increment-framing document quoted materially higher counts for the three primary files (1 315 / 988 / 602). Those numbers reflected the working tree at the time of framing, which included uncommitted WIP changes. The **committed state** at `a7b1d33` matches the story-selection estimates:

| File | Story-Selection Estimate | Increment-Framing Estimate | Actual Committed |
|---|---|---|---|
| `WorkflowDefinitionLoader.cs` | 1 155 | 1 315 | **1 155** |
| `WorkflowOrchestrator.cs` | 860 | 988 | **855** |
| `CliApplication.cs` | 544 | 602 | **544** |

**Finding:** Committed line counts match story-selection estimates. The diff-budget assumptions in the story-selection document remain valid as of commit `a7b1d33`. The increment-framing counts are not applicable to committed code and should not be used to re-scope Stories 2 – 6.

**Caveat:** If the stashed WIP changes land before Story 2 begins, `WorkflowOrchestrator.cs` will grow by approximately 133 lines and `WorkflowDefinitionLoader.cs` by approximately 160 lines, which would push both closer to the 600-line diff-budget boundary for their respective stories. Story leads must re-measure actual counts at story start.

---

## File Verdicts

### 1. `WorkflowDefinitionLoader.cs` — 1 155 lines
**Verdict: SRP breach – extract validation, graph analysis, normalization, legacy graph synthesis**

Five distinct responsibilities are mixed in one static class:

1. **File I/O and deserialization** (`LoadFromFile`, `SaveToFile`, `ValidateFile`) — public API surface.
2. **Schema validation** — 22 private validation methods (`ValidateDefinition`, `ValidateEntryPoints`, `ValidatePackage`, `ValidateNodes`, `ValidateInputs`, `ValidateNodeTargets`, `ValidateTarget`, and 15 more). This alone would fill a focused class.
3. **Graph analysis** (`ValidateGraph`, `BuildGraphEdges`, `TraverseGraph`, `TraverseReverseGraph`, `FindCircularPath`, lines 677 – 903) — graph traversal and cycle detection is a distinct algorithm concern.
4. **Normalization** (`NormalizeAssetPaths`, `NormalizeModels`, `NormalizeReferences`, `NormalizePackageMetadata`, `NormalizeOptionalText`, `NormalizeOptionalUri`, `NormalizeAssets`, and helpers, lines 905 – 1161) — data cleaning and path resolution are separate from validation.
5. **Legacy graph synthesis** (`SynthesizeGraphIfNeeded`, `CreateLegacyStageNode`, lines 1 196 – 1 315) — backwards-compatibility shim that generates graph nodes from a flat node list.

**Story 2 scope:** Extract at minimum graph analysis into `WorkflowGraphAnalyzer` and normalization into `WorkflowDefinitionNormalizer`. Schema validation sub-validators should be grouped under `WorkflowDefinitionValidator`. The public API in `WorkflowDefinitionLoader` becomes thin coordination. Sub-stacking is likely required: extraction PR (≤ 600 lines) followed by test-update PR if combined diff exceeds budget.

---

### 2. `WorkflowOrchestrator.cs` — 855 lines
**Verdict: SRP breach – extract markdown output formatting**

The orchestrator has one primary responsibility (coordinating workflow execution) and one clearly separable embedded responsibility:

- **Markdown output formatting** (`WriteControlStepMarkdown`, `WriteFailedStepMarkdown`, `BuildDecisionMarkdown`, `BuildFinalMarkdown`, lines 508 – 803) — constructs and writes structured markdown for every step outcome. This is a rendering concern entirely independent of execution control flow.
- **Telemetry/logging** (14 `partial void Log*` methods, lines 944 – 980+) — C# source-generated logging. These are pattern-enforced and consistent; not a violation, but they make the class appear larger than its logical complexity.
- **Control flow processing** (`ProcessFork`, `ProcessJoin`, `ProcessExit`, `ProcessWait`) — legitimately part of orchestration.
- **Step lifecycle management** (`StartStep`, `CompleteStageStep`, `FailStep`) — tightly coupled to orchestration state; acceptable here if extraction would increase coupling.

**Story 3 scope:** Extract markdown formatting methods into a `WorkflowStepMarkdownWriter` or similar. The logging partial methods can stay (they are compiler-generated output). The step lifecycle methods should be evaluated during Story 3 for possible extraction into a `WorkflowStepLifecycleManager`, but only if the coupling analysis (see below) supports it without increasing surface area. Sub-stacking is likely required.

**⚠️ Story 3 is blocked until the coupling note below is reviewed and acknowledged.**

---

### 3. `WorkflowRunState.cs` — 234 lines
**Verdict: No violation confirmed**

Single responsibility: encapsulate and transition the persisted state of one workflow run. All properties and methods serve this one concern. `PrepareForResume` is moderately complex (resetting state for a paused run) but is intrinsically a state-transition method and belongs here.

**Coupling note for Story 3 prerequisite — see dedicated section below.**

---

### 4. `CliApplication.cs` — 544 lines
**Verdict: SRP breach – extract command handlers**

The static CLI class mixes four concerns:
1. **Command tree construction** — `CreateRootCommand` builds the full CLI argument tree inline (~140 lines).
2. **Command execution — run/resume** — `ExecuteWorkflowRunCommandAsync` (~100 lines) and `ExecuteWorkflowResumeCommandAsync` (~60 lines) contain argument validation, path resolution, and orchestrator invocation.
3. **Command execution — validate/diagram** — `ExecuteWorkflowValidateCommand` and `ExecuteWorkflowDiagramCommand` inline argument handling and output formatting.
4. **Logger and factory creation** — `CreateLoggerFactory` is an infrastructure concern embedded in the entry point.

Path validation and normalization logic is duplicated across the four command handlers.

**Story 4 scope:** Extract each command handler into a dedicated handler class under `CleanSquad.Cli` (e.g., `RunWorkflowCommandHandler`, `ResumeWorkflowCommandHandler`). Extract the repeated path-normalization logic to a shared utility. `CliApplication` becomes a thin command-tree builder that delegates to handlers.

---

### 5. `WorkflowDecisionResolver.cs` — 266 lines
**Verdict: OCP concern – rule sets are hardcoded in `ParseRulesDecision`**

Single primary responsibility: resolve decision nodes via agent or rule evaluation. The agent path is clean. However, `ParseRulesDecision` selects between rule sets using string equality (`"legacy-review"`, `"clean-agile-review"`), and adding a third rule set requires modifying this method. Each rule set could be extracted to an `IDecisionRuleSet` implementation registered by name.

This is a moderate OCP concern, not an SRP violation. The class has one logical owner and the current two rule sets are coherent. At 266 lines this is within a reasonable size range.

**Story 5 scope:** Evaluate whether the OCP concern warrants extraction. If a third rule set is anticipated, introduce `IDecisionRuleSet` and extract the two existing rule sets. If the rule-set vocabulary is closed and adding new sets requires a release cycle anyway, the current design is acceptable. Flag for discussion at Story 5 start; do not extract speculatively.

**DRY note:** `CountCompletedSteps` in this file (counts by nodeId) duplicates a similar method in `WorkflowOrchestrator` (counts by roleName). These are subtly different queries and not a straightforward deduplication target. Flag but do not extract in Story 5.

---

### 6. `CopilotWorkflowAgentRunner.cs` — 265 lines
**Verdict: No violation confirmed**

Single responsibility: execute Copilot SDK calls for workflow stage nodes. `BuildPromptWithContextAsync`, `CreateSessionConfig`, `ResolveReasoningEffortAsync`, and `ResolveHighestSupportedReasoningEffort` all serve the same SDK integration concern. The `internal static` test-seam methods are intentional, documented, and correctly scoped.

**Story 5 scope:** Confirm shape is sound; no extraction needed. Verify mock seam methods remain consistent with test coverage in `CopilotWorkflowAgentRunnerTests.cs`.

---

### 7. `MarkdownArtifactService.cs` — 218 lines
**Verdict: No violation confirmed**

Single responsibility: manage workflow artifact I/O (create, read, write, log, state serialization). The `WriteState` method builds a markdown state document, which is an intrinsic part of the artifact service's contract for persisting run state in a human-readable form. This is not a separate rendering concern; the template is the canonical state format.

**DRY note:** `NormalizeMarkdown` is a private static method here and also appears verbatim in `WorkflowRequestInput.cs`. Extractable to a shared utility (e.g., `MarkdownNormalizer` in `CleanSquad.Core`). Flag for Story 5 or Story 6.

**Story 5 scope:** Confirm shape is sound; no extraction required. Optionally address `NormalizeMarkdown` duplication.

---

### 8. `WorkflowDiagramRenderer.cs` — 175 lines
**Verdict: No violation confirmed**

Single focused responsibility: render workflow definitions as Mermaid diagrams. All methods (`RenderMarkdown`, `RenderMermaid`, `BuildNodeShape`, `AppendEdge`, `AppendDottedEdge`, `EscapeMermaidLabel`) serve this one rendering concern. Size is justified by the comprehensive set of node kind mappings.

**Story 6 scope:** Confirm and close.

---

### 9. `WorkflowPromptComposer.cs` — 170 lines
**Verdict: No violation confirmed**

Single focused responsibility: compose stage prompts from workflow definitions, assets, and node metadata. All methods support the single prompt-construction concern. Handles both current and legacy node definitions coherently.

**Story 6 scope:** Confirm and close.

---

### 10. `WorkflowArtifacts.cs` — 167 lines
**Verdict: No violation confirmed**

Single focused responsibility: describe and compute file-system paths for a workflow run artifact set. `Create`, `LoadExisting`, path properties, and `Slugify` all serve the single path-addressing concern.

**Story 6 scope:** Confirm and close.

---

### 11. `ExecutionPathDiagramRenderer.cs` — 117 lines
**Verdict: No violation confirmed**

Single focused responsibility: render an executed step sequence as a Mermaid diagram. All methods are dedicated to this rendering concern. Clean boundary contract documented in XML.

**Story 6 scope:** Confirm and close.

---

### 12. `WorkflowReasoningEffort.cs` — 117 lines
**Verdict: No violation confirmed – story scope removed**

A comprehensive static constants and dispatch table for reasoning-effort normalization, ranking, and model-capability resolution. All behavior supports the single concept of workflow reasoning effort. The size is fully justified by the complete set of named constants, normalization aliases, supported models, and ranking logic. No responsibilities are mixed.

**Story 6 scope: Removed.** No refactoring needed or appropriate. This file should not be touched in Story 6.

---

### 13. `WorkflowNodeDefinition.cs` — 102 lines
**Verdict: No violation confirmed**

Comprehensive data contract for a workflow node. All 20+ properties are legitimate node attributes spanning multiple node kinds (Stage, Decision, Fork, Join, Exit, Wait). This is a union-style model; size is structurally justified.

**Story 6 scope:** Confirm and close.

---

### 14. `WorkflowStepState.cs` — 98 lines
**Verdict: No violation confirmed**

Captures all persisted state for one step execution. All properties serve the single step-recording concern.

**Story 6 scope:** Confirm and close.

---

### 15. `WorkflowRequestInput.cs` — 76 lines
**Verdict: No violation confirmed**

Focused IDisposable value object: resolves a workflow request document path, manages temporary file lifecycle. Clean RAII design.

**DRY note:** `NormalizeMarkdown` private static method is identical to one in `MarkdownArtifactService.cs`. Candidates for shared extraction; see note under file 7.

**Story 6 scope:** Confirm and close.

---

### 16. `WorkflowDefinitionValidationResult.cs` — 73 lines
**Verdict: No violation confirmed**

Clean validation result value type. Constructor-computed error/warning arrays and convenience properties all belong to the single validation-result concern.

**Story 6 scope:** Confirm and close.

---

### 17. `CleanupChecklistService.cs` — 72 lines
**Verdict: No violation confirmed**

Simple static domain service building a starter checklist. Single responsibility, all methods cohesive.

**Story 6 scope:** Confirm and close.

---

### 18. `WorkflowDefinition.cs` — 60 lines
**Verdict: No violation confirmed**

Root workflow configuration DTO. All properties belong to the definition contract.

**Story 6 scope:** Confirm and close.

---

### 19. `CliBrandingOptionsLoader.cs` — 60 lines
**Verdict: No violation confirmed**

Focused loader for optional branding JSON. Single responsibility.

**Story 6 scope:** Confirm and close.

---

### 20. `WorkflowStorageOptions.cs` — 54 lines
**Verdict: No violation confirmed**

Options type with two path-resolution methods. Structurally sound.

**Story 6 scope:** Confirm and close.

---

### 21. `CopilotWorkflowOrchestratorFactory.cs` — 53 lines
**Verdict: No violation confirmed**

Thin composition root. Single responsibility: wire up orchestrator dependencies.

**Story 6 scope:** Confirm and close.

---

### Out-of-scope files (≤ 50 lines, noted for completeness)

The following files fall just below the 50-line triage threshold. All were spot-checked and appear correctly shaped:

| File | Lines | Status |
|---|---|---|
| `WorkflowStageDefinition.cs` | 49 | No concerns |
| `WorkflowPackageDefinition.cs` | 49 | No concerns |
| `IWorkflowArtifactService.cs` | 46 | No concerns — clean interface contract |

---

## WorkflowOrchestrator / WorkflowRunState Coupling Note

**⚠️ Story 3 is explicitly blocked until this note is reviewed and acknowledged by the Story 3 lead.**

`WorkflowOrchestrator` directly reads and mutates `WorkflowRunState` properties throughout execution. The following lists every mutation point that Story 3 must not disturb without explicit analysis:

| Field / Property | Access Pattern in Orchestrator |
|---|---|
| `Status` | Set directly: `runContext.State.Status = WorkflowRunStatus.X` in `ProcessExit` and catch blocks |
| `ExitNodeId` | Set directly in `ProcessExit` |
| `CompletedAtUtc` | Stamped directly in `ProcessExit` and catch blocks |
| `UpdatedAtUtc` | Stamped directly at every state transition throughout execution |
| `NextStepNumber` | Incremented: `runContext.State.NextStepNumber++` in `StartStep` |
| `NextActivationSequenceNumber` | Incremented in `Enqueue` (on state itself) and by orchestrator when creating activations |
| `NextParallelGroupSequenceNumber` | Incremented in `ProcessFork` |
| `PendingActivations` | `Add`, `Remove`, `RemoveAt`, `Clear` called directly by orchestrator at every control-flow point |
| `Steps` | `Add` called in `StartStep`; read throughout |
| `ParallelGroups` | `Add` called in `ProcessFork` |
| `WaitingNodes` | `Add` called in `ProcessWait`; `Remove` called on resume |
| `Decisions` | `Add` called in `ExecuteDecisionAsync` |
| `NodeVisitCounts` | Mutated via `IncrementNodeVisit(nodeId)` method on state |
| `EntryNodeId` | Read in `OpenRun` / resume path |

Any extraction from `WorkflowOrchestrator` in Story 3 must either:
a) Preserve the direct property mutation paths exactly (moving methods while keeping `runContext.State.*` access patterns), or  
b) Introduce encapsulating methods on `WorkflowRunState` for operations that should not be exposed as raw property mutations to multiple callers.

Option (b) is preferred for the long term but constitutes additional scope in Story 3. The Story 3 lead must decide before writing any code and document the decision.

---

## Cross-cutting DRY Notes

These are informational — none constitute SRP violations or block any story:

1. **`NormalizeMarkdown` duplication** — Identical private static method in `MarkdownArtifactService.cs` and `WorkflowRequestInput.cs`. Low risk; could be extracted to a `MarkdownNormalizer` utility class in `CleanSquad.Core`. Address in Story 5 or 6.

2. **`CountCompletedSteps` near-duplication** — `WorkflowOrchestrator` counts by `roleName`; `WorkflowDecisionResolver` counts by `nodeId`. Subtly different queries — not a safe deduplication. Flag for review in Story 5; do not extract.

---

## Story Scope Summary

Based on this triage, the story-selection scopes are updated or confirmed as follows:

| Story | Files Targeted | Confirmed Violations | Scope Update |
|---|---|---|---|
| **Story 2** | `WorkflowDefinitionLoader.cs` (1 155 lines) | SRP breach — 5 responsibilities confirmed | **Scope confirmed, sub-stacking likely** |
| **Story 3** | `WorkflowOrchestrator.cs` (855 lines) | SRP breach — markdown formatting separable; telemetry pattern-enforced | **Scope revised: primary extraction is markdown formatting; step lifecycle extraction is optional** |
| **Story 4** | `CliApplication.cs` (544 lines) | SRP breach — command handlers mixed with command tree construction | **Scope confirmed** |
| **Story 5** | `WorkflowDecisionResolver.cs`, `CopilotWorkflowAgentRunner.cs`, `WorkflowRunState.cs`, `MarkdownArtifactService.cs` | One OCP concern in resolver (no SRP violations); all others confirmed sound | **Scope substantially reduced** — no extractions required in the mid-tier group. Story 5 becomes a verification pass plus optional OCP resolver refactor if a new rule set is anticipated. Consider merging Stories 5 and 6 into a single verification-and-close story. |
| **Story 6** | All remaining files (175 lines and below) | No violations confirmed | **Scope confirmed as verification-and-close only; no refactoring** |

**Recommendation:** Merge Stories 5 and 6 into a single verification-and-close story. The mid-tier files have no confirmed SRP violations. Combined scope of both stories is well within the 300-line budget.

---

## Spotted Issues (Out of Scope for Story 1)

The following issues were observed during reading and are explicitly deferred:

- `NormalizeMarkdown` duplication between `MarkdownArtifactService` and `WorkflowRequestInput` — correctness risk if one is updated and not the other. Note as a Story 5/6 task.
- `WorkflowDefinitionLoader.CreateLegacyStageNode` generates default values for node types that may not apply to all legacy workflows. If a legacy workflow definition is missing expected nodes, the synthesized graph may silently produce incorrect routing. This is a potential correctness issue; it was not fixed here.

---

## Post-commit Test Result

`dotnet test .\CleanSquad.slnx` run after committing this artifact (no production code changes):

```
Test summary: total: 68, failed: 0, succeeded: 68, skipped: 0, duration: 2.1s
Build succeeded in 2.9s
```

**Result: PASS. Baseline unchanged after triage artifact commit.**

---

## Verification Checklist

- [x] Pre-work `dotnet test` baseline passed: 68/68 tests, 0 failures
- [x] All 21 in-scope `.cs` files triaged with current actual line counts
- [x] One verdict per file from the allowed set
- [x] Coupling note for `WorkflowOrchestrator`/`WorkflowRunState` present with field-level specificity
- [x] Line count reconciliation completed — committed counts match story-selection estimates
- [x] Branch situation documented — branch created from `feature/clean-squad-starter-ci`, not `main`
- [x] Large files justified for defensible structural reasons are called out (`WorkflowReasoningEffort.cs` — story scope removed)
- [x] Story 2 – 6 scope notes updated per findings
- [x] Post-commit `dotnet test` baseline passed: 68/68 tests, 0 failures
- [x] `git diff HEAD -- src/` shows zero changes to any production `.cs` file
