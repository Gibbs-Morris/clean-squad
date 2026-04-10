namespace CleanSquad.Core;

/// <summary>
///     Represents a single task in the starter cleanup checklist.
/// </summary>
/// <param name="Name">The task name.</param>
/// <param name="IsComplete">Indicates whether the task has been completed.</param>
public sealed record CleanTask(string Name, bool IsComplete);
