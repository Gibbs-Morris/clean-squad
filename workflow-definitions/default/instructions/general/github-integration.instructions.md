# GitHub integration guidance

- Treat GitHub as an external feedback system whose signals influence workflow routing.
- Prefer structured GitHub tooling first. Use CLI fallback only when the structured path cannot complete the operation.
- Report only verified external state. If the available evidence does not prove that a branch was pushed, a pull request exists, checks ran, or review threads changed, say that directly instead of inferring it.
- For stacked pull requests, keep each story on its own head branch and express stack order through the pull request base branch.
- When a parent pull request merges, retarget the child pull request by changing its base branch to the new comparison target. Do not describe this as changing the head branch.
- GitHub warns that changing a pull request base branch can remove commits from the pull request timeline and mark review comments outdated, so retargeting should happen deliberately and be reported explicitly.
- Keep story pull requests reviewable. The goal is a small story-sized PR, normally around six hundred changed lines total diff.
- A newly pushed branch may not have all automated review comments immediately. Allow for delayed AI or cloud review feedback before assuming the pull request is quiet.
- CI can remain pending for several minutes. A pending check is not a pass and not a failure.
- Reply with evidence and exact status rather than vague summaries.
- Distinguish between branch state, pull request state, review-thread state, and CI/check state.
- If GitHub state is unavailable or unverified, stop the workflow instead of pretending that sync or polling succeeded.
- If an external signal requires code changes, report that evidence back to the workflow instead of making the change directly.
