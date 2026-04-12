# CLI Command Reference

Complete reference for every CleanSquad CLI command, argument, option, and flag.

---

## Synopsis

```text
cleansquad [<squad-name>] [command] [options]
```

---

## Global options

| Option | Description |
| --- | --- |
| `--version` | Print the application version and exit. |
| `-?`, `-h`, `--help` | Print help for the current command and exit. |

---

## Root command

```text
cleansquad [<squad-name>]
```

Prints a brief output identifying the squad.

| Argument | Required | Description |
| --- | --- | --- |
| `<squad-name>` | No | Name of the squad to display. Defaults to `CleanSquad`. |

```shell
dotnet run --project src/CleanSquad.Cli -- Delta
```

---

## workflow

Parent command for all workflow operations. Must be followed by a subcommand.

```text
cleansquad workflow <subcommand> [options]
```

---

## workflow run

Start a new workflow run from a request document or inline markdown.

```text
cleansquad workflow run --definition <path> [<request-document> | --request-text <markdown>] [--entry-point <id>] [--workspace-root <path>]
```

| Argument | Required | Description |
| --- | --- | --- |
| `<request-document>` | No* | Path to the request Markdown file (`.md`). Resolved relative to the working directory. |

| Option | Required | Description |
| --- | --- | --- |
| `--definition` | Yes | Path to the `workflow.json` file. Resolved relative to the working directory. |
| `--request-text` | No* | Inline markdown request text for quick runs. Specify this or `<request-document>`, but not both. |
| `--entry-point` | No | ID of the entry point to start from. Defaults to the `defaultEntryPoint` declared in `workflow.json`. |
| `--workspace-root` | No | Absolute or relative path to the workspace root. When omitted, the engine walks up the directory tree to locate it automatically. |

Exactly one of `<request-document>` or `--request-text` must be supplied.

| Exit code | Meaning |
| --- | --- |
| `0` | Run completed successfully. |
| `1` | Validation error, missing file, or execution failure. |

```shell
dotnet run --project src/CleanSquad.Cli -- workflow run `
  --definition workflow-definitions/default/workflow.json `
  workflow-demo/request.md
```

```shell
dotnet run --project src/CleanSquad.Cli -- workflow run `
  --definition workflow-definitions/default/workflow.json `
  --request-text "Fix the flaky CLI test and update the command reference."
```

---

## workflow resume

Resume a previously interrupted workflow run from its saved state.

```text
cleansquad workflow resume <run-path> [--entry-point <id>] [--workspace-root <path>]
```

| Argument | Required | Description |
| --- | --- | --- |
| `<run-path>` | Yes | Path to the run folder produced by a previous `workflow run`. Must contain a valid `state.json`. Resolved relative to the working directory. |

| Option | Required | Description |
| --- | --- | --- |
| `--entry-point` | No | Override the entry point when resuming. Rarely needed. |
| `--workspace-root` | No | Workspace root override. Behaves the same as in `workflow run`. |

| Exit code | Meaning |
| --- | --- |
| `0` | Run resumed and completed successfully. |
| `1` | Validation error, missing state file, or execution failure. |

```shell
dotnet run --project src/CleanSquad.Cli -- workflow resume `
  workflow-runs/20260410-223505-request
```

---

## workflow validate

Validate a workflow definition file without running a workflow.

```text
cleansquad workflow validate --definition <path>
```

| Option | Required | Description |
| --- | --- | --- |
| `--definition` | Yes | Path to the `workflow.json` file to validate. |

Prints a summary of validation results to stdout, reporting each error with its location.
Exits `0` on success, `1` on validation failure.

```shell
dotnet run --project src/CleanSquad.Cli -- workflow validate `
  --definition workflow-definitions/default/workflow.json
```

---

## workflow diagram

Generate a Mermaid flowchart diagram from a workflow definition.

```text
cleansquad workflow diagram --definition <path> [--output <path>]
```

| Option | Required | Description |
| --- | --- | --- |
| `--definition` | Yes | Path to the `workflow.json` file. |
| `--output` | No | Path to write the Markdown diagram file. When omitted, the file is written next to `workflow.json` (e.g. `workflow.diagram.md`). |

Writes a Markdown file containing a Mermaid `flowchart TD` diagram.

```shell
dotnet run --project src/CleanSquad.Cli -- workflow diagram `
  --definition workflow-definitions/default/workflow.json `
  --output docs/diagrams/default-workflow.md
```

---

## Environment

The CLI uses the current working directory to locate the branding configuration file (`clean-squad.cli.json`)
and to resolve all relative paths supplied as arguments or options.
Run the CLI from the repository root for consistent path resolution.
