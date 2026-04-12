# Clean Agile Operating Model

This workflow package models a Clean Agile style of delivery.

## Core ideas

Use these ideas to shape every stage of the workflow:

- Prefer the smallest valuable increment that can produce learning quickly.
- Build shared understanding before implementation.
- Use the Three Amigos perspectives:
  - Business: what problem matters and how value will be recognized.
  - Development: the simplest safe technical approach.
  - Testing: how correctness, edge cases, and regression risk will be checked.
- Treat technical excellence as part of speed, not as a luxury after speed.
- Prefer working software and verified evidence over status language.
- Keep feedback loops short so errors are found early.
- Favor refactoring, simple design, and sustainable pace over large speculative changes.

## Workflow interpretation

The default CleanSquad workflow should behave like this:

1. Frame the increment.
   - Clarify the problem, boundaries, and acceptance signals.
   - Keep the planned scope small enough to deliver and verify safely.
2. Run a Three Amigos pass.
   - Business clarifies user value, examples, and completion intent.
   - Development clarifies the smallest sound implementation approach.
   - Testing clarifies validation strategy, failure modes, and regression coverage.
3. Implement the increment.
   - Make the smallest coherent change that satisfies the framed increment.
   - Preserve code quality, tests, and maintainability.
4. Review the result.
   - Judge the work by correctness, clarity, verification, and technical quality.
5. Rework only when needed.
   - Apply focused corrections, then re-review quickly.

## Working style

- Prefer precise examples to vague interpretations.
- Prefer small, testable changes to large rewrites.
- Prefer evidence-backed approval to optimistic assumptions.
- Avoid process theater: each stage should reduce risk, improve understanding, or verify the result.
- If the requested change is too large for one safe increment, explicitly recommend narrowing the slice.
