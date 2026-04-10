namespace CleanSquad.Core.Workflows;

/// <summary>
///     Represents the machine-readable decision extracted from the reviewer markdown.
/// </summary>
/// <param name="Approved">Whether the reviewer approved the submitted build.</param>
/// <param name="RawContent">The complete markdown content returned by the reviewer.</param>
public sealed record WorkflowReviewDecision(bool Approved, string RawContent);
