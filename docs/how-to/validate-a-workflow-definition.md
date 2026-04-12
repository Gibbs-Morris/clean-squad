# How to validate a workflow definition

Use this guide when you want to check that a `workflow.json` file is structurally correct
before running a workflow or committing changes to it.

---

## Prerequisites

- The solution has been built: `dotnet build CleanSquad.slnx`
- You have a `workflow.json` file to validate

---

## Steps

### 1. Run the validate command

```shell
dotnet run --project src/CleanSquad.Cli -- workflow validate \
  --definition <path-to-workflow.json>
```

For example, to validate the default definition:

```shell
dotnet run --project src/CleanSquad.Cli -- workflow validate \
  --definition workflow-definitions/default/workflow.json
```

### 2. Read the output

If the definition is valid:

```text
Validation passed. 0 errors.
```

If errors are present, each one is listed with a description:

```text
Validation failed. 3 errors:
  - Node 'builder' references undefined node 'undefined-node' in 'next'.
  - Entry point 'build' references undefined node 'missing-builder'.
  - Shared asset path 'instructions/general/missing.md' does not exist.
```

### 3. Fix any reported errors

Open the `workflow.json` in a text editor and correct the issues listed.
Re-run the validate command after each fix to confirm the error is resolved.

---

## What the validator checks

The validator inspects the definition for structural integrity:

- All `next` references on nodes point to valid node IDs
- All entry point `nodeId` values point to valid node IDs
- All shared asset and node asset paths exist on disk relative to the definition file
- All `Fork` nodes have a matching `Join` node
- All `Decision` nodes declare at least one `choice`
- The `defaultEntryPoint` ID is declared in the `entryPoints` list

The validator does **not** run any agents or execute any workflow logic.

---

## When to validate

- Before committing changes to a workflow definition
- After adding new nodes, entry points, or assets
- When diagnosing a run that failed at startup with a validation error

---

## Related

- [Run a workflow](run-a-workflow.md)
- [Generate a workflow diagram](generate-a-workflow-diagram.md)
- [CLI command reference](../reference/commands.md)
