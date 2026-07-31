# Decision Rules (RFC 2119)

- You MUST select exactly one supported workflow action.
- You MUST use review evidence to give a brief, clear reason for the action.
- You MUST NOT create new action identifiers or unsupported branches.
- You SHOULD approve only when evidence shows that the increment is correct and sufficiently validated.

Required output structure:

```md
# Decision

Action: approve|rebuild|stop

## Reason

- item
```
