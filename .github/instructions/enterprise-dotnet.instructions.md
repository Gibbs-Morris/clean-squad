---
description: "Use when creating or editing enterprise-grade .NET code, project files, or shared build configuration with strong architecture, documentation, testability, and observability expectations."
name: "CleanSquad Enterprise .NET Quality Rules"
applyTo: "**/*.{cs,csproj,props,targets,slnx,json}"
---
# CleanSquad Enterprise .NET Quality Rules

These rules supplement the baseline .NET starter guidance. They use RFC 2119 terminology and MUST be interpreted accordingly.

## Enterprise quality baseline

- Code MUST be designed for enterprise-grade maintainability, correctness, operability, and change resilience.
- Prototype-style shortcuts, ambiguous ownership, and "good enough for now" design decisions MUST NOT be normalized into production code.
- Each change MUST leave the codebase clearer, safer, or more diagnosable than it was before.

## Instruction set composition

- These rules MUST be applied together with the baseline starter rules and the specialized instruction files in `.github/instructions/`.
- Use `naming-and-vertical-slice.instructions.md` for feature-centric naming, deterministic placement, and vertical-slice structure decisions.
- Use `project-organization.instructions.md` for project boundaries, minimal project-file guidance, and future abstraction-project decisions.
- Use `domain-modeling-basics.instructions.md` for business-behavior organization, domain-oriented vocabulary, and testable workflow or core design.

## Separation of concerns

- Projects, types, and methods MUST have clear, focused responsibilities and MUST NOT accumulate unrelated concerns.
- Business logic MUST live in the appropriate reusable domain or core layer rather than being embedded in orchestration, UI, transport, or hosting code.
- Composition roots, command-line entry points, and workflow orchestration code MUST remain thin and MUST delegate business behavior to purpose-built collaborators.
- Cross-cutting concerns such as logging, metrics, tracing, validation, and configuration MUST be implemented in ways that preserve clear boundaries between responsibilities.
- Feature ownership, folder placement, and namespace structure SHOULD make those boundaries visible from the repository layout, not only from the implementation details inside a class.

## Testability is non-negotiable

- Testability MUST be treated as a first-class design requirement from the start of every change.
- New or changed behavior MUST be verifiable through deterministic automated tests.
- Types and methods MUST be structured so that important behavior can be exercised without fragile global state, hidden time dependencies, or uncontrolled external effects.
- Tight coupling, static-only coordination, and hard-wired infrastructure dependencies MUST NOT be introduced when they make business behavior materially harder to test.
- When an edge case makes full automated coverage impractical, the design SHOULD still preserve clear seams so the behavior remains isolated, reviewable, and observable.
- Domain-oriented collaborators, workflow state transitions, and reusable policies SHOULD be structured so they can be exercised independently of the command-line host.

## Apply SOLID, then DRY, then KISS

- Design decisions MUST apply SOLID first, DRY second, and KISS third.
- Simplification MUST NOT collapse important abstractions, responsibilities, or extension seams that are required for sound design.
- Deduplication MUST NOT merge concepts that should remain distinct for clarity, correctness, or future change safety.
- "Simple" code MUST still be explicit enough for maintainers and agents to understand intent, boundaries, and failure modes.

## Documentation and XML comments

- Public types, public members, and externally significant contracts MUST have high-quality XML documentation.
- XML documentation MUST explain what the code does, the meaning of important inputs and outputs, notable side effects, and behavior that would otherwise require code archaeology.
- Important internal extension points, workflow boundaries, integration seams, and non-obvious behavior SHOULD also be documented when they are likely to be maintained by humans or agents.
- Documentation MUST be updated in the same change that modifies the documented behavior.
- Reviews MUST verify that every touched documented member still has accurate, useful, and intentional XML documentation after the change.
- Comments and documentation MUST NOT become placeholders, paraphrases of the symbol name, or stale descriptions that mislead maintainers.

## Observability and OpenTelemetry

- Logging, metrics, and traces MUST be treated as first-class concerns in the design of production code.
- New services, workflows, background operations, and external integrations MUST be designed so their behavior can be diagnosed through structured logs, metrics, and traces.
- OpenTelemetry-compatible instrumentation MUST be preferred for new observability work.
- Failure paths, degraded states, retries, and boundary crossings MUST emit enough telemetry to support diagnosis in real environments.
- Correlation and context propagation MUST be preserved across meaningful operation boundaries where the platform and architecture allow it.
- When full instrumentation cannot be added immediately, the design SHOULD preserve clear seams for near-term telemetry integration rather than baking in opaque behavior.