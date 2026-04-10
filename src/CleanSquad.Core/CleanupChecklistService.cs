using System.Collections.Generic;

namespace CleanSquad.Core;

/// <summary>
///     Creates the starter checklist used by the CleanSquad command-line experience.
/// </summary>
public static class CleanupChecklistService
{
    /// <summary>
    ///     Builds the default starter checklist for a squad name.
    /// </summary>
    /// <param name="squadName">The squad name to normalize.</param>
    /// <returns>A deterministic starter checklist.</returns>
    public static IReadOnlyList<CleanTask> CreateStarterChecklist(string? squadName)
    {
        string normalizedSquadName = NormalizeSquadName(squadName);

        return
        [
            new CleanTask($"Define the mission for {normalizedSquadName}", false),
            new CleanTask("Keep the build green", false),
            new CleanTask("Ship with confidence", false)
        ];
    }

    /// <summary>
    ///     Creates a summary line for the starter checklist.
    /// </summary>
    /// <param name="squadName">The squad name to normalize.</param>
    /// <returns>A summary describing the starter checklist.</returns>
    public static string CreateSummary(string? squadName)
    {
        string normalizedSquadName = NormalizeSquadName(squadName);
        IReadOnlyList<CleanTask> checklist = CreateStarterChecklist(normalizedSquadName);

        return $"{normalizedSquadName}: {checklist.Count} starter tasks ready.";
    }

    private static string NormalizeSquadName(string? squadName)
    {
        return string.IsNullOrWhiteSpace(squadName) ? "CleanSquad" : squadName.Trim();
    }
}
