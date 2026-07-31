# Clean Agile Workflow Rules (RFC 2119)

- The workflow MUST frame each request as the smallest valuable increment that agents can implement and verify safely.
- If one clean increment cannot contain the request, the workflow MUST classify the request as an epic.
- The workflow MUST divide each epic into ordered stories before implementation starts.
- The workflow MUST deliver an epic through small, reviewable story pull requests.
- Story pull requests SHOULD contain approximately 600 changed lines in the total diff.
- For stacked pull requests, the workflow MUST identify each parent and child relationship.
- The workflow MUST use controlled base-branch retargeting for stacked pull requests.
- The workflow MUST complete applicable architecture and solution design before implementation.
- Architecture is applicable when the request makes an important change to system shape, boundaries, integrations, or implementation structure.
- The workflow MUST use business, development, and testing perspectives to establish shared understanding before implementation.
- The workflow MUST review architecture, collaboration outputs, and code early in the delivery process.
- The workflow MUST include technical excellence, testing, and simple design in normal delivery work.
- The workflow MUST prefer short feedback loops and evidence-based decisions to large speculative changes.
- The workflow MUST use working, validated output as the primary approval evidence.
- The workflow SHOULD use focused rework when reviewers give feedback.
- The workflow SHOULD identify requested work that is too large for one clean increment.
