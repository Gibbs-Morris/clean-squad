using System;
using System.Collections.Generic;
using System.Globalization;

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
    /// <param name="options">The optional checklist configuration.</param>
    /// <returns>A deterministic starter checklist.</returns>
    public static IReadOnlyList<CleanTask> CreateStarterChecklist(string? squadName, CleanupChecklistOptions? options = null)
    {
        CleanupChecklistOptions effectiveOptions = NormalizeOptions(options);
        return CreateStarterChecklistCore(NormalizeSquadName(squadName, effectiveOptions), effectiveOptions);
    }

    /// <summary>
    ///     Creates a summary line for the starter checklist.
    /// </summary>
    /// <param name="squadName">The squad name to normalize.</param>
    /// <param name="options">The optional checklist configuration.</param>
    /// <returns>A summary describing the starter checklist.</returns>
    public static string CreateSummary(string? squadName, CleanupChecklistOptions? options = null)
    {
        CleanupChecklistOptions effectiveOptions = NormalizeOptions(options);
        string normalizedSquadName = NormalizeSquadName(squadName, effectiveOptions);
        IReadOnlyList<CleanTask> checklist = CreateStarterChecklistCore(normalizedSquadName, effectiveOptions);

        return FormatSummary(effectiveOptions.SummaryTemplate, normalizedSquadName, checklist.Count);
    }

    private static IReadOnlyList<CleanTask> CreateStarterChecklistCore(string normalizedSquadName, CleanupChecklistOptions options)
    {
        return
        [
            new CleanTask(FormatSquadTemplate(options.MissionTaskTemplate, normalizedSquadName), false),
            new CleanTask(options.BuildTaskName, false),
            new CleanTask(options.ReleaseTaskName, false)
        ];
    }

    private static CleanupChecklistOptions NormalizeOptions(CleanupChecklistOptions? options)
    {
        CleanupChecklistOptions source = options ?? new CleanupChecklistOptions();
        return new CleanupChecklistOptions
        {
            DefaultSquadName = NormalizeText(source.DefaultSquadName, "CleanSquad"),
            MissionTaskTemplate = NormalizeText(source.MissionTaskTemplate, "Define the mission for {SquadName}"),
            BuildTaskName = NormalizeText(source.BuildTaskName, "Keep the build green"),
            ReleaseTaskName = NormalizeText(source.ReleaseTaskName, "Ship with confidence"),
            SummaryTemplate = NormalizeText(source.SummaryTemplate, "{SquadName}: {TaskCount} starter tasks ready."),
        };
    }

    private static string NormalizeSquadName(string? squadName, CleanupChecklistOptions options)
    {
        return string.IsNullOrWhiteSpace(squadName) ? options.DefaultSquadName : squadName.Trim();
    }

    private static string FormatSquadTemplate(string template, string squadName)
    {
        return template.Replace("{SquadName}", squadName, StringComparison.Ordinal);
    }

    private static string FormatSummary(string template, string squadName, int taskCount)
    {
        return template.Replace("{SquadName}", squadName, StringComparison.Ordinal)
            .Replace("{TaskCount}", taskCount.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    private static string NormalizeText(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
