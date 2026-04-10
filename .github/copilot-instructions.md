# Project Guidelines

## Reasoning and task interpretation

- For every task, prefer first-principles thinking before acting.
- Reduce requests to their core goal, constraints, and desired outcome instead of following wording mechanically.
- Optimize for user intent, especially when the request contains typos, shorthand, or ambiguous phrasing.
- Make sure the request is well understood before making changes.

## Verification

- Use a chain-of-verification mindset for every task.
- Check important assumptions, claims, edits, and outcomes before considering the work complete.
- Prefer verified correctness over fast but weak answers.
- Confirm the final result fully addresses the request and does not omit important requirements.

## Repository operating rules

- Read `.github/copilot-instructions.md` and every file under `.github/instructions/` before making changes.
- Keep the repository split cleanly:
	- `src/` contains production .NET code.
	- `tests/` contains automated tests.
	- `key-principles/` remains the documentation knowledge base.
- Treat `Directory.Build.props`, `Directory.Packages.props`, `global.json`, and `CleanSquad.slnx` as the authoritative .NET build configuration.
- Keep package versions centralized in `Directory.Packages.props`; project-level `PackageReference` items must not declare `Version` attributes.
- Keep the command-line project thin and move business logic into `CleanSquad.Core` wherever practical.
- Prefer deterministic unit tests with xUnit; keep tests fast, isolated, and easy to understand.
- Use `dotnet restore`, `dotnet build`, and `dotnet test` against `CleanSquad.slnx` unless a task explicitly introduces dedicated scripts.
- Do not add GitHub Actions workflows or CI automation unless the task explicitly asks for them.
