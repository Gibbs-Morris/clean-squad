using System;
using CleanSquad.Core;

namespace CleanSquad.Cli;

/// <summary>
///     Defines the optional CLI branding configuration.
/// </summary>
internal sealed class CliBrandingOptions
{
    /// <summary>
    ///     Gets or sets the CLI application name.
    /// </summary>
    public string ApplicationName { get; set; } = "CleanSquad";

    /// <summary>
    ///     Gets or sets the root command description.
    /// </summary>
    public string? ApplicationDescription { get; set; }

    /// <summary>
    ///     Gets or sets the workflow command description.
    /// </summary>
    public string? WorkflowCommandDescription { get; set; }

    /// <summary>
    ///     Gets or sets the cleanup checklist text configuration.
    /// </summary>
    public CleanupChecklistOptions Checklist { get; set; } = new();
}
