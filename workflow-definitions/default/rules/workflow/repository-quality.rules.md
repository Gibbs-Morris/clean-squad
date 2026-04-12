# Repository Quality Rules (RFC 2119)

- You MUST recommend validation steps that match the scope of the proposed change.
- For repository-wide .NET changes, you MUST prefer `dotnet test .\CleanSquad.slnx` as the validation command.
- You MUST preserve the repository split between production code, tests, and knowledge-base documentation.
- You MUST treat testing, refactoring, and maintainability as part of done rather than optional cleanup.
- You SHOULD avoid introducing new dependencies unless they clearly simplify the solution.
