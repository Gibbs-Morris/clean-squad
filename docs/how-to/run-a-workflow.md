# How to run a workflow

Use this guide when you want to start a new workflow run from either a request document or inline markdown.

---

## Prerequisites

- The solution has been built: `dotnet build CleanSquad.slnx`
- You have a `workflow.json` definition file (the default is at `workflow-definitions/default/workflow.json`)
- You have either a request document in Markdown format (`.md`) or a short request you want to pass inline

---

## Steps

### 1. Validate the definition first

It is good practice to confirm the definition is valid before spending time on a run:

```shell
dotnet run --project src/CleanSquad.Cli -- workflow validate \
  --definition workflow-definitions/default/workflow.json
```

Proceed only when validation passes with 0 errors.

### 2. Run the workflow from a file

```shell
dotnet run --project src/CleanSquad.Cli -- workflow run \
  --definition <path-to-workflow.json> \
  <path-to-request.md>
```

Replace `<path-to-workflow.json>` and `<path-to-request.md>` with your actual paths.
Both paths are resolved relative to the current working directory.

### 3. Run the workflow from inline markdown

For quick requests, you can pass markdown directly on the command line instead of creating a separate file:

```shell
dotnet run --project src/CleanSquad.Cli -- workflow run \
  --definition workflow-definitions/default/workflow.json \
  --request-text "Fix the flaky CLI test and update the docs."
```

Specify exactly one of `<path-to-request.md>` or `--request-text`.

### 4. Optionally specify an entry point

If the workflow definition declares multiple entry points and you want to start from somewhere other than the default:

```shell
dotnet run --project src/CleanSquad.Cli -- workflow run \
  --definition workflow-definitions/default/workflow.json \
  my-request.md \
  --entry-point build
```

Available entry point IDs are listed in the `entryPoints` array of `workflow.json`.

### 5. Optionally override the workspace root

If your CLI working directory is not the repository root, supply the workspace root explicitly:

```shell
dotnet run --project src/CleanSquad.Cli -- workflow run \
  --definition workflow-definitions/default/workflow.json \
  my-request.md \
  --workspace-root /path/to/repo
```

---

## What to expect

The CLI creates a timestamped run folder under `workflow-runs/` (for example, `workflow-runs/20260412-090000-my-request/`).
It writes stage outputs to that folder as each node completes.
When the run finishes, the CLI prints a summary and exits with code `0`.

---

## Troubleshooting

- **"File not found" for the definition** — check that the path is relative to the directory you ran the command from, and that the file ends in `.json`.
- **"File not found" for the request** — same check; also confirm the file ends in `.md`.
- **"Specify either a markdown (.md) request document path or --request-text, but not both."** — remove one of the two request inputs.
- **Exit code `1` with an error message** — read the error message in the terminal output; it will describe which validation step failed.

---

## Related

- [Resume an interrupted workflow](resume-a-workflow.md)
- [Validate a workflow definition](validate-a-workflow-definition.md)
- [CLI command reference](../reference/commands.md)
