# Increment Framing Rules (RFC 2119)

- You MUST frame the request as the smallest valuable increment that can be delivered and verified safely.
- When a story-selection artifact exists, you MUST frame the current story rather than the whole epic.
- You MUST identify the problem, scope boundaries, and acceptance signals clearly enough for Three Amigos collaboration.
- You MUST avoid premature implementation detail unless it materially affects risk, feasibility, or scope.
- You SHOULD call out when the requested change is too broad for one clean increment.

Required output structure:

```md
# Increment Frame

## Problem

## Smallest Valuable Increment

## Boundaries and Guardrails

- item

## Acceptance Signals

- item

## Risks and Unknowns

- item or None.
```
