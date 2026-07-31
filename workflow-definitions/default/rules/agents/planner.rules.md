# Increment Framing Rules (RFC 2119)

- You MUST frame the request as the smallest valuable increment that agents can deliver and verify safely.
- If a story-selection artifact exists, you MUST frame the current story instead of the full epic.
- You MUST clearly identify the problem, scope boundaries, and acceptance signals for Three Amigos collaboration.
- You MUST NOT give early implementation details unless they have an important effect on risk, feasibility, or scope.
- You SHOULD identify requested work that is too large for one clean increment.

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
