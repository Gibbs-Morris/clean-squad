# CleanSquad CLI Documentation

Documentation for the CleanSquad command-line interface, organised by the [Diátaxis](https://diataxis.fr/) framework.

---

> **New to CleanSquad? Start here →** [Run your first workflow](tutorial/run-your-first-workflow.md)
> Validate a definition, write a request, and complete a full workflow run from scratch. Takes about ten minutes.

---

## Tutorial

Learning-oriented. Start here if you are new to the CLI.

- [Run your first workflow](tutorial/run-your-first-workflow.md)
  — Validate a definition, write a request, and complete a full workflow run from scratch.

---

## How-to guides

Goal-oriented. Pick the guide for the task you need to complete right now.

- [Run a workflow](how-to/run-a-workflow.md)
  — Start a new workflow run from a request document or inline markdown.
- [Resume an interrupted workflow](how-to/resume-a-workflow.md)
  — Continue a run that stopped before completing.
- [Validate a workflow definition](how-to/validate-a-workflow-definition.md)
  — Check that a `workflow.json` file is structurally correct.
- [Generate a workflow diagram](how-to/generate-a-workflow-diagram.md)
  — Produce a Mermaid flowchart from a workflow definition.
- [Configure CLI branding](how-to/configure-cli-branding.md)
  — Run the CLI under a custom application name and descriptions.

---

## Reference

Information-oriented. Look up exact command syntax, options, and configuration.

- [Commands](reference/commands.md)
  — Every command, argument, option, exit code, and example.
- [Branding configuration](reference/branding-configuration.md)
  — The `clean-squad.cli.json` file schema and field descriptions.

---

## Explanation

Understanding-oriented. Read these when you want to know *why* things work the way they do.

- [Workflow model](explanation/workflow-model.md)
  — Nodes, graph execution, persistent state, entry points, and the default Clean Agile workflow.
- [CLI design](explanation/cli-design.md)
  — Design goals, command dispatch, branding, working directory behaviour, and logging.
