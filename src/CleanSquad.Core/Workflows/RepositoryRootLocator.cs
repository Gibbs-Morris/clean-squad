using System;
using System.IO;

namespace CleanSquad.Core.Workflows;

/// <summary>
///     Locates the repository root by walking up the directory tree.
/// </summary>
public static class RepositoryRootLocator
{
    /// <summary>
    ///     Finds the repository root from a starting path.
    /// </summary>
    /// <param name="startPath">The starting directory or file path.</param>
    /// <returns>The located repository root path.</returns>
    public static string FindRepositoryRoot(string startPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(startPath);

        string normalizedStartPath = Path.GetFullPath(startPath);
        DirectoryInfo? currentDirectory = Directory.Exists(normalizedStartPath)
            ? new DirectoryInfo(normalizedStartPath)
            : new DirectoryInfo(Path.GetDirectoryName(normalizedStartPath) ?? normalizedStartPath);

        while (currentDirectory is not null)
        {
            if (File.Exists(Path.Combine(currentDirectory.FullName, "CleanSquad.slnx")))
            {
                return currentDirectory.FullName;
            }

            currentDirectory = currentDirectory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the CleanSquad repository root. Expected to find CleanSquad.slnx in the current directory or an ancestor.");
    }
}
