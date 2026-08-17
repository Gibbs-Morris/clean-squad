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
- Set shared agent execution preferences under top-level `agentDefaults`; use node properties only when a stage or
  agent-backed decision needs an override.
- Keep agent persona separate from rules.
- Keep reusable theory or reasoning guidance separate from repository guidance.
- Express enforceable constraints as RFC 2119 rules using MUST, SHOULD, and MUST NOT.
- Prefer shared assets for cross-cutting guidance and node assets for role-specific behavior.
- Use ASD-STE100 Simplified Technical English for natural-language workflow instructions and outputs.
- Preserve literal code, commands, paths, identifiers, schemas, logs, quoted text, and required output tokens.

## Agent model inheritance

Agent-backed `Stage` nodes and `Decision` nodes with `decisionMode: "Agent"` inherit omitted `models`,
`reasoningEffort`, and `responseTimeout` values from top-level `agentDefaults`.

```json
{
  "agentDefaults": {
    "models": ["gpt-5.6-sol"],
    "reasoningEffort": "high",
    "responseTimeout": "00:10:00"
  },
  "nodes": [
    {
      "id": "fast-stage",
      "kind": "Stage",
      "models": ["gpt-5.6-luna"]
    },
    {
      "id": "provider-default-stage",
      "kind": "Stage",
      "inheritAgentDefaults": false
    }
  ]
}
```

Overrides are resolved per property: the `fast-stage` model list replaces the inherited list while its reasoning
effort and timeout still inherit. Setting `inheritAgentDefaults` to `false` disables all inheritance for that node.
Model lists are ordered preferences; the runner selects the first model available to the current Copilot account.
A one-item list therefore requires that model rather than silently choosing another one.

## Default package model

The `default/` package is the canonical CleanSquad workflow and SHOULD model a Clean Agile delivery loop.

In practice that means the default graph should:

- classify high-level work as either a single story or an epic before implementation begins
- frame the smallest valuable increment first
- decompose epic-sized work into ordered, reviewable stories with explicit dependency and stacked-PR guidance
- add architecture and solution design work before implementation when the request materially changes system shape, boundaries, or code structure
- establish shared understanding through the Three Amigos perspectives (business, development, testing)
- shift specialist review left so architecture, collaboration outputs, and code all receive feedback before the latest possible moment
- implement with technical excellence and fast feedback
- review against working, validated output
- rework in small focused loops when necessary
- communicate requirements, plans, findings, and instructions in ASD-STE100 Simplified Technical English
