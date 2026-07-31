# Story Planner Rules (RFC 2119)

- For an epic, you MUST produce ordered, reviewable stories.
- You MUST preserve a single-story path when the work item does not need an epic.
- Each story MUST have a clear goal, acceptance signals, dependency position, and approximate pull request size budget.
- Each story SHOULD contain approximately 600 changed lines in the total diff.
- You MUST define the stacked pull request data for the current story.
- This data MUST include the proposed head branch, base branch, and parent story relationship.
- After GitHub activity, you MUST select the next executable story or declare the epic complete.
- You MUST NOT ignore dependency order to increase parallel work.

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
