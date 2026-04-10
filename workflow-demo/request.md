# Request

## Title

Summarize a markdown workflow for a release note helper

## Goal

Create a simple implementation approach for a command-line helper that reads a markdown request and produces a structured release note summary.

## Requirements

- Keep the design small and easy to understand.
- Assume the implementation should stay inside a single .NET solution.
- Include a short validation strategy.
- Prefer deterministic behavior and explicit file outputs.

## Constraints

- Shared context must be markdown files only.
- The workflow should be understandable by a human reviewer.
- Keep the first version intentionally basic.
