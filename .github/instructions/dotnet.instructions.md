---
description: "Use when creating or editing .NET code, project files, or shared build configuration for the CleanSquad starter."
name: "CleanSquad .NET Starter Rules"
applyTo: "**/*.{cs,csproj,props,targets,slnx,json}"
---
# CleanSquad .NET Starter Rules

- Keep production code under `src/CleanSquad.*`.
- Keep tests under `tests/CleanSquad.*.UnitTests`.
- Prefer file-scoped namespaces, nullable reference types, and explicit `using` directives.
- Keep command-line orchestration in `CleanSquad.Cli` minimal; move reusable behavior into `CleanSquad.Core`.
- Keep package versions in `Directory.Packages.props` only.
- Keep shared analyzer and compiler settings in `Directory.Build.props` unless a project has a strong reason to diverge.
- Add new tests with xUnit and keep them deterministic, focused, and fast.
- Use `CleanSquad.slnx` as the canonical solution file.
- Do not add workflows, deployment automation, or packaging infrastructure until a task explicitly asks for them.