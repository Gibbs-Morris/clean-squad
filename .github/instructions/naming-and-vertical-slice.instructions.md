---
description: "Use when organizing .NET code by feature, choosing names, or deciding where files and namespaces belong in CleanSquad."
name: "CleanSquad Naming and Vertical Slice Rules"
applyTo: "**/*.cs"
---
# CleanSquad Naming and Vertical Slice Rules

These rules supplement the baseline .NET starter guidance. They use RFC 2119 terminology and MUST be interpreted accordingly.

## Governing summary

- CleanSquad source code SHOULD be organized around features, workflows, and bounded responsibilities rather than broad technical buckets.
- Namespaces, folders, and file locations SHOULD make ownership obvious without requiring code archaeology.
- When a structure choice is ambiguous, prefer the layout that keeps related behavior, contracts, and tests easiest to find together.

## Feature-centric organization

- New code SHOULD be grouped by feature, workflow capability, or bounded responsibility before it is grouped by technical artifact type.
- Horizontal catch-all buckets such as `Services`, `Models`, `Helpers`, `Utils`, and `Common` MUST NOT be introduced unless the term is genuinely part of the repository's ubiquitous language.
- `CleanSquad.Cli` MUST remain a thin composition and command-line layer. Reusable behavior SHOULD live in `CleanSquad.Core` or `CleanSquad.Workflow`.
- Cross-cutting infrastructure SHOULD still be placed intentionally. A folder such as `Infrastructure` is acceptable when it contains infrastructure-specific behavior with a clear boundary, as in `CleanSquad.Workflow.Infrastructure`.

## Deterministic namespace and folder placement

- A source file's namespace SHOULD mirror its folder path beneath the project root unless there is a strong reason to keep the type at the project root.
- Root-level namespaces are appropriate only for small, foundational types that are central to the whole project.
- If a type naturally belongs to a named feature area, its file MUST move with that feature instead of remaining at the project root for convenience.
- Tests SHOULD mirror the production vertical structure when a feature area contains multiple related types.

### Repository examples

- `src/CleanSquad.Workflow/Definitions/WorkflowDefinition.cs` maps cleanly to `namespace CleanSquad.Workflow.Definitions;`.
- `src/CleanSquad.Workflow/Orchestration/WorkflowOrchestrator.cs` maps cleanly to `namespace CleanSquad.Workflow.Orchestration;`.
- `tests/CleanSquad.Workflow.UnitTests/Definitions/WorkflowDefinitionLoaderTests.cs` mirrors the `Definitions` slice under `CleanSquad.Workflow`.
- Root-level files such as `src/CleanSquad.Core/CleanTask.cs` and `src/CleanSquad.Workflow/WorkflowExecutionRequest.cs` are acceptable only while they remain genuinely central and few in number.

## Vertical slices beat horizontal buckets

- When a folder begins to collect many unrelated files, it SHOULD be split by feature or subdomain before introducing generic type buckets.
- As a practical guide, once a folder grows beyond roughly 20 same-kind source files or becomes difficult to scan, split it into narrower vertical slices.
- Prefer structures such as `Workflows/Definitions`, `Workflows/Prompting`, or `Cleanup/Checklist` over structures such as `Services`, `DTOs`, or `Utilities`.
- If a new feature needs commands, handlers, validators, reducers, or tests, keep those artifacts close to the feature they serve instead of scattering them across project-wide technical folders.

## Naming rules

- Public and internal type names MUST use clear PascalCase names.
- Interfaces MUST use the `I` prefix.
- Boolean members SHOULD start with `Is`, `Has`, `Can`, or `Should` when that improves readability.
- Method names SHOULD be verbs or verb phrases that describe observable behavior.
- Options types SHOULD end with `Options`.
- Exception types SHOULD end with `Exception`.
- Test classes SHOULD end with `Tests` and SHOULD be named after the unit or behavior they verify.
- Names MUST communicate intent and ownership more strongly than implementation detail.

## Placement decision guide

- Put a type at the project root only when it is foundational to the whole assembly.
- Put a type under a named folder when its responsibility is primarily owned by one feature, workflow capability, or bounded concern.
- Create a new vertical slice before creating a new generic horizontal bucket.
- If you cannot decide where a file belongs without saying "it is used everywhere," verify that claim. Shared usage alone is not enough to justify `Common` or `Helpers`.

## Documentation expectations for names and placement

- Public types and externally significant contracts MUST have XML documentation that matches the chosen name and placement.
- Folder and namespace names SHOULD make the codebase easier for both humans and agents to navigate.
- When renaming or moving a type, update related XML documentation, tests, and nearby examples in the same change.

## Avoid these anti-patterns

- Do not create a project-wide `Services` folder just because a new class performs work.
- Do not create a `Models` folder that mixes domain types, DTOs, options, and runtime state.
- Do not keep feature-specific code at the project root once the feature has multiple collaborating types.
- Do not mirror technical layers so aggressively that understanding one feature requires opening five unrelated folders.