using System;
using System.IO;
using System.Text.Json;
using CleanSquad.Core;

namespace CleanSquad.Cli;

/// <summary>
///     Loads and normalizes optional CLI branding configuration from the workspace.
/// </summary>
internal static class CliBrandingOptionsLoader
{
    private const string DefaultApplicationName = "CleanSquad";
    private const string DefaultWorkflowCommandDescription = "Run the workflow engine.";
    private const string DefaultConfigFileName = "clean-squad.cli.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>
    ///     Loads the branding configuration from the provided directory when present.
    /// </summary>
    /// <param name="directoryPath">The directory to inspect for the branding file.</param>
    /// <returns>The effective branding options.</returns>
    public static CliBrandingOptions Load(string directoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);

        string normalizedDirectoryPath = Path.GetFullPath(directoryPath);
        string configFilePath = Path.Combine(normalizedDirectoryPath, DefaultConfigFileName);
        if (!File.Exists(configFilePath))
        {
            return Normalize(new CliBrandingOptions());
        }

        try
        {
            CliBrandingOptions? configuration = JsonSerializer.Deserialize<CliBrandingOptions>(File.ReadAllText(configFilePath), SerializerOptions);
            return Normalize(configuration);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"The CLI branding configuration '{configFilePath}' is invalid.", exception);
        }
    }

    private static CliBrandingOptions Normalize(CliBrandingOptions? options)
    {
        CliBrandingOptions source = options ?? new CliBrandingOptions();
        string applicationName = NormalizeText(source.ApplicationName, DefaultApplicationName);
        return new CliBrandingOptions
        {
            ApplicationName = applicationName,
            ApplicationDescription = NormalizeText(source.ApplicationDescription, $"{applicationName} command-line interface."),
            WorkflowCommandDescription = NormalizeText(source.WorkflowCommandDescription, DefaultWorkflowCommandDescription),
            Checklist = source.Checklist ?? new CleanupChecklistOptions { DefaultSquadName = applicationName },
        };
    }

    private static string NormalizeText(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
