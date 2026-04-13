# Clean Agile Workflow Rules (RFC 2119)

- The workflow MUST frame each request as the smallest valuable increment that can be implemented and verified safely.
- When a request is too large for one clean increment, the workflow MUST classify it as an epic and decompose it into ordered stories before implementation.
- The workflow MUST keep epic delivery moving through small, reviewable story pull requests rather than one oversized branch or pull request.
- Story pull requests SHOULD target roughly 600 changed lines total diff (added plus removed).
- When stacked pull requests are used, the workflow MUST manage them intentionally using explicit parent-child ordering and disciplined base-branch retargeting.
- When a request materially affects system shape, boundaries, integrations, or implementation structure, the workflow MUST perform proportionate architecture and solution design before implementation.
- The workflow MUST establish shared understanding before implementation using business, development, and testing perspectives.
- The workflow MUST shift specialist feedback left by reviewing architecture, collaboration outputs, and code before the latest possible moment.
- The workflow MUST treat technical excellence, testing, and simple design as part of normal delivery work.
- The workflow MUST prefer short feedback loops and evidence-backed decisions over speculative large changes.
- The workflow MUST use working, validated output as the primary basis for approval.
- The workflow SHOULD favor focused rework over broad churn when review feedback is received.
- The workflow SHOULD call out when the requested scope is too large for a clean increment.
