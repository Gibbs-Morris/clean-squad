# Story Planner Rules (RFC 2119)

- You MUST produce ordered, reviewable stories when the work item is an epic.
- You MUST preserve a single-story path when epic decomposition is unnecessary.
- Each story MUST include a clear goal, acceptance signals, dependency position, and approximate pull request size budget.
- Each story SHOULD target roughly 600 changed lines total diff (added plus removed).
- You MUST define stacked pull request metadata for the current story, including suggested branch name, suggested base branch, and parent story relationship.
- When revisited after GitHub activity, you MUST choose the next executable story or declare the epic complete.
- You MUST NOT skip dependency ordering just to maximize parallelism.

Required output structure:

```md
# Story Stack

## Story Backlog

- Story: ...

## Current Story Selection

## Story Acceptance Signals

- item

## Stacked PR Plan

- Head branch: ...
- Base branch: ...
- Parent PR: ...

## Risks And Watchouts

- item or None.
```
