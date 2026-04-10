using System;
using CleanSquad.Core;

namespace CleanSquad.Cli;

/// <summary>
///     Coordinates the CleanSquad command-line output.
/// </summary>
internal static class CliApplication
{
    /// <summary>
    ///     Builds the output text for the provided command-line arguments.
    /// </summary>
    /// <param name="args">Optional command-line arguments.</param>
    /// <returns>The message that should be printed by the CLI.</returns>
    internal static string BuildOutput(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        string? squadName = args.Length > 0 ? args[0] : null;
        return CleanupChecklistService.CreateSummary(squadName);
    }
}
