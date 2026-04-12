# How to configure CLI branding

Use this guide when you want to run the CleanSquad CLI under a custom application name
and command descriptions without modifying source code.

---

## Prerequisites

- The solution has been built: `dotnet build CleanSquad.slnx`
- You know which working directory you run the CLI from (usually the repository root)

---

## Steps

### 1. Create the branding file

In the directory from which you run the CLI, create a file named `clean-squad.cli.json`.

```shell
# From the repository root
New-Item clean-squad.cli.json
```

### 2. Add your custom values

Open `clean-squad.cli.json` and add the fields you want to override:

```json
{
  "applicationName": "DeliveryBot",
  "workflowCommandDescription": "Run and manage DeliveryBot workflow pipelines.",
  "workflowRunCommandDescription": "Start a new DeliveryBot workflow from a request document.",
  "workflowResumeCommandDescription": "Resume an interrupted DeliveryBot workflow run.",
  "workflowValidateCommandDescription": "Validate a DeliveryBot workflow definition.",
  "workflowDiagramCommandDescription": "Generate a diagram from a DeliveryBot workflow definition."
}
```

All fields are optional. Omit any field you do not need to change.

### 3. Verify the branding is applied

Run the CLI help command to confirm the custom descriptions appear:

```shell
dotnet run --project src/CleanSquad.Cli -- --help
dotnet run --project src/CleanSquad.Cli -- workflow --help
```

The output should show your custom application name and descriptions.

---

## What each field controls

| Field | Where it appears |
| --- | --- |
| `applicationName` | Root command description header and squad name output |
| `workflowCommandDescription` | `--help` output for the `workflow` subcommand |
| `workflowRunCommandDescription` | `--help` output for `workflow run` |
| `workflowResumeCommandDescription` | `--help` output for `workflow resume` |
| `workflowValidateCommandDescription` | `--help` output for `workflow validate` |
| `workflowDiagramCommandDescription` | `--help` output for `workflow diagram` |

---

## Notes

- The branding file is read once at startup. Restart the CLI after changing the file.
- If the file is missing the CLI starts with built-in default values. No error is printed.
- A JSON parse error in the file will cause the CLI to exit with code `1` and an error message.

---

## Related

- [Branding configuration reference](../reference/branding-configuration.md)
- [CLI command reference](../reference/commands.md)
