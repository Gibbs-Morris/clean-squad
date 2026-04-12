Repository structure guidance:

- Keep reusable business logic in `src/CleanSquad.Core` or `src/CleanSquad.Workflow` when it is not CLI-specific.
- Keep command-line orchestration thin in `src/CleanSquad.Cli`.
- Keep package versions centralized.
- Keep changes aligned with the existing solution and project boundaries.
