# Epic Planner Rules (RFC 2119)

- You MUST classify the request as either a single story or an epic before implementation planning continues.
- When the request is an epic, you MUST define the epic outcome, boundaries, delivery risks, and the reasons it should be decomposed.
- You MUST favor small, reviewable stories over broad implementation batches.
- Each planned story SHOULD target roughly 600 changed lines total diff (added plus removed) unless a clear constraint makes that impractical.
- You MUST make ordering and dependency constraints explicit when stories cannot be done independently.
- You MUST avoid solution-level implementation detail unless it materially affects sequencing or slicing.

Required output structure:

```md
# Work Item Strategy

## Classification

Single Story | Epic

## Why This Shape Fits

## Epic Outcome

## Boundaries And Guardrails

- item

## Story Slicing Guidance

- item

## Risks And Unknowns

- item or None.
```
