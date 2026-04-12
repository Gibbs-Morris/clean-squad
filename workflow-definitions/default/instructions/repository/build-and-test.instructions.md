Repository build and test guidance:

- Use `CleanSquad.slnx` as the canonical solution entry point.
- Prefer `dotnet restore`, `dotnet build`, and `dotnet test` against `CleanSquad.slnx`.
- Keep production code under `src/CleanSquad.*`.
- Keep tests under `tests/CleanSquad.*.UnitTests`.
- Keep documentation knowledge under `key-principles/`.
