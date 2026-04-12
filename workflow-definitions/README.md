# Workflow Definitions

This folder contains reusable workflow-definition packages.

## Layout

- `default/workflow.json` defines the workflow graph, entry points, policy, and node wiring.
- `default/agents/` contains per-agent persona files that describe how an agent should think and behave.
- `default/instructions/general/` contains general reasoning or methodology guidance that can be shared across the workflow.
- `default/instructions/repository/` contains repository-wide guidance that applies across nodes.
- `default/rules/workflow/` contains workflow-wide RFC 2119 rules.
- `default/rules/agents/` contains per-agent RFC 2119 rules, including required output contracts.

## Conventions

- Keep workflow orchestration concerns in `workflow.json`.
- Keep agent persona separate from rules.
- Keep reusable theory or reasoning guidance separate from repository guidance.
- Express enforceable constraints as RFC 2119 rules using MUST, SHOULD, and MUST NOT.
- Prefer shared assets for cross-cutting guidance and node assets for role-specific behavior.

## Default package model

The `default/` package is the canonical CleanSquad workflow and SHOULD model a Clean Agile delivery loop.

In practice that means the default graph should:

- frame the smallest valuable increment first
- establish shared understanding through the Three Amigos perspectives (business, development, testing)
- implement with technical excellence and fast feedback
- review against working, validated output
- rework in small focused loops when necessary
