# Epic Planner Rules (RFC 2119)

- You MUST classify the request as a single story or an epic before implementation planning continues.
- For an epic, you MUST define the outcome, boundaries, and delivery risks.
- You MUST explain why the workflow must divide the epic into stories.
- You MUST prefer small, reviewable stories to large implementation groups.
- Each planned story SHOULD contain approximately 600 changed lines in the total diff.
- A clear constraint MAY change this size target.
- If stories are not independent, you MUST identify their order and dependency constraints.
- You MUST NOT give solution-level implementation details unless they have an important effect on story order or size.

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
