# CleanSquad

CleanSquad is a minimal .NET starter repository that now contains two things side by side:

- a simple `CleanSquad` solution with core business logic, a command-line app, and unit tests
- the existing `key-principles/` knowledge base for reasoning, documentation, and agent guidance

The codebase is intentionally small to start with, but the repository structure follows the same kind of centralized .NET setup used by larger production-ready solutions:

- `global.json` pins the SDK
- `Directory.Build.props` centralizes shared MSBuild and analyzer settings
- `Directory.Packages.props` centralizes NuGet package versions
- `CleanSquad.slnx` is the canonical solution file

## Repository layout

- `src/CleanSquad.Core` — core business logic
- `src/CleanSquad.Cli` — command-line entry point
- `tests/CleanSquad.Core.UnitTests` — unit tests for the core library
- `tests/CleanSquad.Cli.UnitTests` — unit tests for the CLI layer
- `key-principles/` — self-contained documentation topics and explanations

## Prerequisites

- .NET SDK 10.0.103 or later with patch roll-forward enabled by `global.json`

## Getting started

Restore packages:

`dotnet restore CleanSquad.slnx`

Build the solution:

`dotnet build CleanSquad.slnx`

Run the tests:

`dotnet test CleanSquad.slnx`

Run the CLI:

`dotnet run --project src/CleanSquad.Cli/CleanSquad.Cli.csproj -- Delta`

## Notes

- The starter is intentionally minimal and designed to build and test cleanly from day one.
- GitHub Actions workflows now cover clean build, unit tests, and ReSharper cleanup checks.
- The `key-principles/` documentation area continues to follow its own repo-specific documentation rules.
