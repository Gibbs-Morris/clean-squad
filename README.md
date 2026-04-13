# CleanSquad

[![Unit Tests](https://github.com/Gibbs-Morris/clean-squad/actions/workflows/unit-tests.yml/badge.svg)](https://github.com/Gibbs-Morris/clean-squad/actions/workflows/unit-tests.yml)
[![Build (perfect)](https://github.com/Gibbs-Morris/clean-squad/actions/workflows/full-build.yml/badge.svg)](https://github.com/Gibbs-Morris/clean-squad/actions/workflows/full-build.yml)
[![ReSharper Cleanup Check](https://github.com/Gibbs-Morris/clean-squad/actions/workflows/cleanup.yml/badge.svg)](https://github.com/Gibbs-Morris/clean-squad/actions/workflows/cleanup.yml)

CleanSquad is a .NET workflow CLI that runs structured, graph-based agent workflows. You define a workflow as a directed graph of named nodes — each node carrying a role, a prompt, and declared inputs and outputs — and the CLI orchestrates execution: resolving the graph, invoking each node in order, persisting state between runs, and producing a final output. The repository also ships a knowledge base of engineering principles and a rich documentation tree organized by the Diátaxis framework.

## Repository layout

| Path | Purpose |
|---|---|
| `src/CleanSquad.Core` | Reusable business and domain logic shared across the solution |
| `src/CleanSquad.Cli` | Command-line entry point and composition root |
| `src/CleanSquad.Workflow` | Workflow definitions, graph-based orchestration, prompting, and run persistence |
| `tests/CleanSquad.Core.UnitTests` | Unit tests for the core library |
| `tests/CleanSquad.Cli.UnitTests` | Unit tests for the CLI layer |
| `tests/CleanSquad.Workflow.UnitTests` | Unit tests for the workflow engine |
| `key-principles/` | Self-contained documentation topics covering engineering reasoning and agent guidance |
| `docs/` | Full CLI documentation organized by the Diátaxis framework |

The build configuration is centralized:

- `global.json` — pins the .NET SDK version (10.0.103, with patch roll-forward)
- `Directory.Build.props` — shared MSBuild, analyzer, and compiler settings
- `Directory.Packages.props` — centralized NuGet package versions
- `CleanSquad.slnx` — canonical solution file

## Prerequisites

- .NET SDK 10.0.103 or later (roll-forward behaviour controlled by `global.json`)

## Getting started

```sh
# Restore packages
dotnet restore CleanSquad.slnx

# Build the solution
dotnet build CleanSquad.slnx

# Run all tests
dotnet test CleanSquad.slnx

# Run the CLI
dotnet run --project src/CleanSquad.Cli/CleanSquad.Cli.csproj -- Delta
```

## Documentation

> **New to CleanSquad?** Start with the [Run your first workflow](docs/tutorial/run-your-first-workflow.md) tutorial — it walks you through a complete run in about ten minutes.

Full CLI documentation lives in [`docs/README.md`](docs/README.md) and is organized by the [Diátaxis](https://diataxis.fr/) framework:

- **Tutorial** — step-by-step guide for running your first workflow
- **How-to guides** — goal-oriented recipes (run, resume, validate, diagram, configure branding)
- **Reference** — exact command syntax, options, exit codes, and configuration schema
- **Explanation** — the workflow model, graph execution, CLI design goals, and branding behaviour

## Contributing

- All production code lives under `src/CleanSquad.*`; tests live under `tests/CleanSquad.*.UnitTests`.
- Keep package versions in `Directory.Packages.props`; do not add `Version` attributes to individual `PackageReference` items.
- The repository enforces a zero-warning build. CI runs a clean build, unit tests, and a ReSharper cleanup check on every push.
- Use `CleanSquad.slnx` as the solution entry point for all `dotnet` commands.
