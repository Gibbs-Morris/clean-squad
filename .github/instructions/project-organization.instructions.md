---
description: "Use when creating or editing .NET project files, shared build configuration, or assembly boundaries in CleanSquad."
name: "CleanSquad Project Organization Rules"
applyTo: "**/*.{csproj,props,targets,slnx,json}"
---
# CleanSquad Project Organization Rules

These rules supplement the baseline .NET starter guidance. They use RFC 2119 terminology and MUST be interpreted accordingly.

## Governing summary

- Project files MUST stay minimal and inherit shared behavior from the repository build configuration whenever possible.
- Build, analyzer, package, and solution conventions MUST remain centralized.
- Assembly boundaries SHOULD communicate responsibilities clearly and SHOULD evolve deliberately rather than through accidental sprawl.

## Shared build authorities

- `Directory.Build.props`, `Directory.Packages.props`, `global.json`, and `CleanSquad.slnx` MUST be treated as the authoritative shared build configuration for this repository.
- Project-level `PackageReference` items MUST NOT declare `Version` attributes because package versions are centralized in `Directory.Packages.props`.
- Shared analyzer, compiler, documentation, and test defaults SHOULD live in `Directory.Build.props` unless a project has a strong and documented reason to diverge.
- New build configuration SHOULD be added centrally first and only pushed down into a project when central configuration would be incorrect or harmful.

## Keep project files thin

- A `.csproj` file SHOULD contain only the settings and references that are specific to that project.
- Redundant defaults inherited from shared props files SHOULD NOT be restated in each project.
- If a project begins to accumulate many custom conditions, package rules, or repository-wide behaviors, move that logic into shared props or targets files instead of duplicating it.

## Assembly boundaries and responsibilities

- `CleanSquad.Cli` SHOULD remain the command-line entry point and composition layer.
- `CleanSquad.Core` SHOULD hold reusable business and domain-adjacent behavior that is not specific to the host process.
- `CleanSquad.Workflow` SHOULD hold workflow-definition, orchestration, prompting, storage, and workflow runtime behavior.
- New assemblies SHOULD be added only when they create a clearer boundary, improve reuse, or reduce coupling in a durable way.
- A new project MUST NOT be introduced merely to satisfy speculative architecture.

## Future project naming guidance

- Existing project names such as `CleanSquad.Cli`, `CleanSquad.Core`, and `CleanSquad.Workflow` are authoritative today and MUST NOT be renamed unless a task explicitly requires it.
- When adding a new project, prefer names that communicate role and responsibility clearly.
- For future assemblies, the repository SHOULD favor role-based names such as `<Feature>.Abstractions`, `<Feature>.Core`, `<Feature>.Runtime`, `<Feature>.Client`, or `<Feature>.TestHarness` when those roles are genuinely present.
- Technology-first names SHOULD be avoided when a responsibility-based name would be clearer and more stable.

## Abstractions projects

- A `*.Abstractions` project SHOULD be created only when contracts must be shared across assemblies or host boundaries without dragging implementation dependencies along.
- `*.Abstractions` projects MUST contain contracts, messages, and other dependency-light public shapes only.
- Implementation code, infrastructure wiring, and host-specific behavior MUST remain in non-abstraction projects.
- If an abstraction would only be used by one assembly, prefer keeping the contract close to that assembly until a real sharing need appears.

## Quality gates and verification

- Changes to project or build files MUST preserve a clean `dotnet build` and `dotnet test` against `CleanSquad.slnx`.
- Zero-warning intent MUST be preserved across production and test projects.
- New project structures SHOULD be easy for humans and agents to understand from the solution layout alone.