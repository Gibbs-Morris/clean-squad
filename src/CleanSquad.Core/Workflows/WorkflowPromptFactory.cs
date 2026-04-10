using System;
using System.IO;

namespace CleanSquad.Core.Workflows;

/// <summary>
///     Builds the deterministic prompts used for each workflow stage.
/// </summary>
public static class WorkflowPromptFactory
{
    /// <summary>
    ///     Creates the planner prompt.
    /// </summary>
    /// <param name="artifacts">The workflow artifacts for the run.</param>
    /// <returns>The planner prompt.</returns>
    public static string CreatePlannerPrompt(WorkflowArtifacts artifacts)
    {
        ArgumentNullException.ThrowIfNull(artifacts);

        return $"""
You are the planning agent in a simple software delivery workflow.
    Use only the markdown file content that appears later in this prompt.
- {Path.GetFileName(artifacts.RequestMarkdownPath)}

Do not use tools.
    Do not assume any context beyond the provided markdown content.
Return markdown only and do not wrap the response in code fences.

Use this exact structure:
# Plan
## Goal
## Deliverables
- item
## Steps
1. step
## Risks
- item
## Acceptance Criteria
- item
""";
    }

    /// <summary>
    ///     Creates the builder prompt.
    /// </summary>
    /// <param name="artifacts">The workflow artifacts for the run.</param>
    /// <returns>The builder prompt.</returns>
    public static string CreateBuilderPrompt(WorkflowArtifacts artifacts)
    {
        ArgumentNullException.ThrowIfNull(artifacts);

        return $"""
You are the builder agent in a simple software delivery workflow.
    Use only the markdown file content that appears later in this prompt.
- {Path.GetFileName(artifacts.RequestMarkdownPath)}
- {Path.GetFileName(artifacts.PlanMarkdownPath)}

Do not use tools.
    Do not assume any context beyond the provided markdown content.
Return markdown only and do not wrap the response in code fences.

Use this exact structure:
# Build
## Summary
## Proposed Changes
- item
## Implementation Notes
- item
## Test Notes
- item
## Open Questions
- item or None.
""";
    }

    /// <summary>
    ///     Creates the reviewer prompt.
    /// </summary>
    /// <param name="artifacts">The workflow artifacts for the run.</param>
    /// <returns>The reviewer prompt.</returns>
    public static string CreateReviewerPrompt(WorkflowArtifacts artifacts)
    {
        ArgumentNullException.ThrowIfNull(artifacts);

        return $"""
You are the reviewer agent in a simple software delivery workflow.
    Use only the markdown file content that appears later in this prompt.
- {Path.GetFileName(artifacts.RequestMarkdownPath)}
- {Path.GetFileName(artifacts.PlanMarkdownPath)}
- {Path.GetFileName(artifacts.BuildMarkdownPath)}

Review the build against the request and the plan.
Do not use tools.
    Do not assume any context beyond the provided markdown content.
Return markdown only and do not wrap the response in code fences.

Use this exact structure:
# Review
Approved: yes|no
## Verdict
## Findings
- item or None.
## Builder Instructions
- item or None.

If the build is good enough, set Approved: yes and keep Findings and Builder Instructions minimal.
If the build has issues, set Approved: no and provide actionable instructions for the builder.
""";
    }

    /// <summary>
    ///     Creates the rebuild prompt.
    /// </summary>
    /// <param name="artifacts">The workflow artifacts for the run.</param>
    /// <returns>The rebuild prompt.</returns>
    public static string CreateRebuilderPrompt(WorkflowArtifacts artifacts)
    {
        ArgumentNullException.ThrowIfNull(artifacts);

        return $"""
You are the builder agent revising a previous build after reviewer feedback.
    Use only the markdown file content that appears later in this prompt.
- {Path.GetFileName(artifacts.RequestMarkdownPath)}
- {Path.GetFileName(artifacts.PlanMarkdownPath)}
- {Path.GetFileName(artifacts.BuildMarkdownPath)}
- {Path.GetFileName(artifacts.ReviewMarkdownPath)}

Do not use tools.
    Do not assume any context beyond the provided markdown content.
Address the review feedback directly.
Return markdown only and do not wrap the response in code fences.

Use this exact structure:
# Rebuild
## Summary
## Changes Made
- item
## Remaining Risks
- item or None.
## Validation Notes
- item
""";
    }
}
