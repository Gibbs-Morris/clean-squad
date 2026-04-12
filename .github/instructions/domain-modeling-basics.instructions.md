---
description: "Use when organizing business behavior, workflow behavior, and feature logic inside CleanSquad's production .NET code."
name: "CleanSquad Domain Modeling Basics"
applyTo: "src/**/*.cs"
---
# CleanSquad Domain Modeling Basics

These rules supplement the baseline .NET starter guidance. They use RFC 2119 terminology and MUST be interpreted accordingly.

## Governing summary

- Business behavior MUST be organized so its ownership, dependencies, and test seams are clear.
- Vertical slices SHOULD keep related behavior, contracts, and state transitions close together.
- CleanSquad MAY use domain-oriented concepts such as commands, handlers, events, reducers, and policies when they make the behavior easier to understand, test, and evolve.

## Keep the domain model intentional

- Business behavior MUST NOT be hidden inside command-line entry points, ad hoc static coordinators, or infrastructure-only types.
- If a behavior is reusable or central to the product, it SHOULD live in `CleanSquad.Core` or `CleanSquad.Workflow` instead of `CleanSquad.Cli`.
- Types SHOULD be grouped by the feature or workflow capability that owns the behavior.
- Domain concepts SHOULD be introduced because they clarify intent, not because a pattern catalog said they looked impressive.

## Practical vertical-slice vocabulary

- A **command** represents an intention to perform an action.
- A **handler** coordinates the work needed to fulfill one command or decision.
- An **event** represents a fact that already happened.
- A **reducer** or state-transition function applies known facts to produce new state in a deterministic way.
- A **policy** represents a rule that influences routing, validation, or next-step decisions.

These concepts are tools, not mandatory ceremony. Use them when they make a slice easier to understand.

## CleanSquad-specific examples

- `CleanupChecklistService` is currently a small domain service at the `CleanSquad.Core` root because the cleanup checklist behavior is still compact and central.
- `WorkflowDefinitionLoader` fits naturally in `CleanSquad.Workflow.Definitions` because it belongs to the workflow-definition slice.
- `WorkflowOrchestrator` fits naturally in `CleanSquad.Workflow.Orchestration` because it coordinates workflow execution rather than representing domain data itself.
- If workflow behavior grows to include distinct commands, policies, and state transitions, keep those artifacts close to the relevant workflow slice instead of scattering them across project-wide helper folders.

## Testability-by-design

- Important behavior MUST be testable through deterministic automated tests.
- Time, file system, network, agent, and other external effects SHOULD be introduced through explicit seams.
- `TimeProvider` or equivalent abstractions SHOULD be preferred over hard-coded calls to ambient time when behavior depends on time.
- Static coordination, hidden global state, and service-locator style access MUST NOT be introduced when they materially reduce testability.
- When a type mixes orchestration with business rules, extract the business rules into a reusable collaborator before the design hardens.

## Naming guidance for domain-oriented artifacts

- Commands SHOULD be named with clear verb-plus-object intent, such as `{Verb}{Noun}`.
- Handlers SHOULD be named after the behavior they handle, such as `{CommandName}Handler` or `{DecisionName}Resolver`, when that clarifies ownership.
- Events SHOULD use past-tense or fact-style names when they represent completed outcomes.
- Policies, rules, and validators SHOULD have names that describe the rule being enforced rather than the mechanism used.

## Avoid these anti-patterns

- Do not introduce commands, handlers, or events as empty ceremony around trivial pass-through code.
- Do not bury domain rules inside `Program`, `CliApplication`, or infrastructure glue just because that is where the current call path begins.
- Do not mix unrelated workflow-definition, prompting, storage, and orchestration logic into one catch-all type.
- Do not import Orleans-specific, event-sourcing-specific, or framework-specific patterns unless the repository actually adopts those technologies in a later task.