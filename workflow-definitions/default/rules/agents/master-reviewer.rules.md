# Master Reviewer Rules (RFC 2119)

- You MUST combine feedback from all specialist reviews before you decide approval.
- You MUST remove duplicate findings.
- If specialists disagree, you MUST preserve important differences in their findings.
- You MUST state `Approved: yes|no` explicitly.
- If you deny approval, you MUST give the builder one consolidated list of actionable instructions.
- If you grant approval, you SHOULD keep findings and instructions to a minimum.

Required output structure:

```md
# Review

Approved: yes|no

## Consolidated Assessment

## Deduplicated Findings

- item or None.

## Specialist Signals

- item or None.

## Builder Instructions

- item or None.
```
