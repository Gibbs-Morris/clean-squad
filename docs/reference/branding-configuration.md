# Branding Configuration Reference

The CleanSquad CLI supports optional branding via a JSON configuration file.
This file allows teams to run the CLI under a custom application name and command descriptions
without modifying source code.

---

## File location

Place the file in the **working directory** from which the CLI is invoked.

**Filename:** `clean-squad.cli.json`

---

## Schema

```json
{
  "applicationName": "string",
  "workflowCommandDescription": "string",
  "workflowRunCommandDescription": "string",
  "workflowResumeCommandDescription": "string",
  "workflowValidateCommandDescription": "string",
  "workflowDiagramCommandDescription": "string"
}
```

All fields are optional. Fields that are absent or empty fall back to their built-in defaults.

---

## Fields

| Field | Default | Description |
| --- | --- | --- |
| `applicationName` | `CleanSquad` | The name shown in the root command description and branding output. |
| `workflowCommandDescription` | *(built-in)* | Description shown in `--help` for the `workflow` subcommand. |
| `workflowRunCommandDescription` | *(built-in)* | Description shown in `--help` for `workflow run`. |
| `workflowResumeCommandDescription` | *(built-in)* | Description shown in `--help` for `workflow resume`. |
| `workflowValidateCommandDescription` | *(built-in)* | Description shown in `--help` for `workflow validate`. |
| `workflowDiagramCommandDescription` | *(built-in)* | Description shown in `--help` for `workflow diagram`. |

---

## Example

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

---

## Behavior

- If the file does not exist the CLI starts normally using all built-in defaults.
- If the file exists but is empty or contains only some fields, missing fields fall back to defaults.
- The file is read once at startup. Changes to the file do not take effect until the CLI is re-invoked.
- The file is expected to be valid JSON. A parse error will cause the CLI to report an error and exit with code `1`.
