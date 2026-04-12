---
description: "Use when creating or editing .NET code, project files, or shared build configuration for the CleanSquad starter."
name: "CleanSquad .NET Starter Rules"
applyTo: "**/*.{cs,csproj,props,targets,slnx,json}"
---
# CleanSquad .NET Starter Rules

- Keep production code under `src/CleanSquad.*`.
- Keep tests under `tests/CleanSquad.*.UnitTests`.
- Prefer file-scoped namespaces, nullable reference types, and explicit `using` directives.
- Use `naming-and-vertical-slice.instructions.md` for feature-centric naming, folder placement, and vertical-slice organization decisions.
- Use `project-organization.instructions.md` for project-file structure, shared build authorities, and future assembly-boundary guidance.
- Use `domain-modeling-basics.instructions.md` when organizing reusable business or workflow behavior inside production code.
- Keep command-line orchestration in `CleanSquad.Cli` minimal; move reusable behavior into `CleanSquad.Core`.
- Keep package versions in `Directory.Packages.props` only.
- Keep shared analyzer and compiler settings in `Directory.Build.props` unless a project has a strong reason to diverge.
- Add new tests with xUnit and keep them deterministic, focused, and fast.
- Use `CleanSquad.slnx` as the canonical solution file.
- Do not add workflows, deployment automation, or packaging infrastructure until a task explicitly asks for them.