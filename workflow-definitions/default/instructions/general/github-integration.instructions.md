# GitHub integration guidance

- Treat GitHub as an external feedback system whose signals influence workflow routing.
- Prefer structured GitHub tooling first. Use CLI fallback only when the structured path cannot complete the operation.
- A newly pushed branch may not have all automated review comments immediately. Allow for delayed AI or cloud review feedback before assuming the pull request is quiet.
- CI can remain pending for several minutes. A pending check is not a pass and not a failure.
- Reply with evidence and exact status rather than vague summaries.
- Distinguish between branch state, pull request state, review-thread state, and CI/check state.
- If an external signal requires code changes, report that evidence back to the workflow instead of making the change directly.
