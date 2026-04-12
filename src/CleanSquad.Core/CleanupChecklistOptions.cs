namespace CleanSquad.Core;

/// <summary>
///     Configures the starter checklist and summary text shown by the CLI experience.
/// </summary>
public sealed class CleanupChecklistOptions
{
    /// <summary>
    ///     Gets or sets the default squad or product name.
    /// </summary>
    public string DefaultSquadName { get; set; } = "CleanSquad";

    /// <summary>
    ///     Gets or sets the task template used for the mission item.
    /// </summary>
    public string MissionTaskTemplate { get; set; } = "Define the mission for {SquadName}";

    /// <summary>
    ///     Gets or sets the second starter task label.
    /// </summary>
    public string BuildTaskName { get; set; } = "Keep the build green";

    /// <summary>
    ///     Gets or sets the third starter task label.
    /// </summary>
    public string ReleaseTaskName { get; set; } = "Ship with confidence";

    /// <summary>
    ///     Gets or sets the summary template.
    /// </summary>
    public string SummaryTemplate { get; set; } = "{SquadName}: {TaskCount} starter tasks ready.";
}
