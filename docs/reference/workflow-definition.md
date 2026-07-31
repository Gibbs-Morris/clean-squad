# Workflow definition reference

This reference covers agent execution settings in `workflow.json`. Graph fields, node transitions, and policy fields
continue to use their existing workflow model.

## Workflow agent defaults

The optional top-level `agentDefaults` object defines values inherited by every agent-backed node.

| Property | Type | Default | Meaning |
| --- | --- | --- | --- |
| `models` | string array | `[]` | Ordered model identifiers. The first model available to the current Copilot account is selected. |
| `reasoningEffort` | string or null | `null` | `low`, `medium`, `high`, `xhigh`, or `highest-supported`. |
| `responseTimeout` | string or null | `null` | Positive .NET `TimeSpan` value, such as `00:10:00`. |

```json
{
  "agentDefaults": {
    "models": ["gpt-5.6-sol"],
    "reasoningEffort": "high",
    "responseTimeout": "00:10:00"
  }
}
```

An empty model list delegates model selection to the provider. A single-item list requires that model. Multiple
items provide an ordered fallback chain. The special `auto` value deliberately delegates selection to Copilot when
it is reached in that chain.

## Node overrides

`Stage` nodes and `Decision` nodes with `decisionMode: "Agent"` support the same three execution properties plus an
inheritance switch.

| Property | Type | Default | Meaning |
| --- | --- | --- | --- |
| `inheritAgentDefaults` | boolean | `true` | Whether omitted execution properties inherit from `agentDefaults`. |
| `models` | string array | `[]` | Replaces the inherited ordered list when non-empty. Lists are not merged. |
| `reasoningEffort` | string or null | `null` | Replaces the inherited reasoning value when present. |
| `responseTimeout` | string or null | `null` | Replaces the inherited timeout when present. |

Resolution is per property. For example, this builder uses its own ordered models while retaining the workflow
reasoning effort and timeout:

```json
{
  "agentDefaults": {
    "models": ["gpt-5.6-sol"],
    "reasoningEffort": "high",
    "responseTimeout": "00:10:00"
  },
  "nodes": [
    {
      "id": "builder",
      "kind": "Stage",
      "models": ["gpt-5.6-terra", "auto"]
    }
  ]
}
```

Set `inheritAgentDefaults` to `false` to make every omitted property fall back to the provider or runner default:

```json
{
  "id": "provider-default-stage",
  "kind": "Stage",
  "inheritAgentDefaults": false
}
```

Rules decisions, forks, joins, waits, and exits never inherit agent settings because they do not invoke the agent
runner.

## Validation and runtime availability

Definition validation is offline. It checks model identifiers for blanks, validates reasoning and timeout syntax,
and evaluates inheritance when checking `highest-supported`. It does not assume a hard-coded provider model list.

At execution time, the runner asks Copilot for the current account model catalogue and selects the first configured
match. If no preference is available, execution fails with the configured and available identifiers in the error.
This accounts for model rollouts, subscription differences, and organisation policy without making workflow
validation depend on external services.
