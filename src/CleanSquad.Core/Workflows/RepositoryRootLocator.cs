using System;
using System.IO;
using System.Linq;

namespace CleanSquad.Core.Workflows;

/// <summary>
///     Locates the most appropriate workspace root by walking up the directory tree.
/// </summary>
public static class RepositoryRootLocator
{
    /// <summary>
    ///     Finds the most appropriate workspace root from a starting path.
    /// </summary>
    /// <param name="startPath">The starting directory or file path.</param>
    /// <returns>The located workspace root path, or the starting directory when no markers are found.</returns>
    public static string FindRepositoryRoot(string startPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(startPath);

        string normalizedStartPath = Path.GetFullPath(startPath);
        DirectoryInfo startingDirectory = Directory.Exists(normalizedStartPath)
            ? new DirectoryInfo(normalizedStartPath)
            : new DirectoryInfo(Path.GetDirectoryName(normalizedStartPath) ?? normalizedStartPath);
        DirectoryInfo? currentDirectory = startingDirectory;

        while (currentDirectory is not null)
        {
            if (LooksLikeWorkspaceRoot(currentDirectory))
            {
                return currentDirectory.FullName;
            }

            currentDirectory = currentDirectory.Parent;
        }

        return startingDirectory.FullName;
    }

    private static bool LooksLikeWorkspaceRoot(DirectoryInfo directory)
    {
        return Directory.Exists(Path.Combine(directory.FullName, ".git"))
            || File.Exists(Path.Combine(directory.FullName, "Directory.Build.props"))
            || File.Exists(Path.Combine(directory.FullName, "Directory.Packages.props"))
            || File.Exists(Path.Combine(directory.FullName, "global.json"))
            || Directory.EnumerateFiles(directory.FullName, "*.sln", SearchOption.TopDirectoryOnly).Any()
            || Directory.EnumerateFiles(directory.FullName, "*.slnx", SearchOption.TopDirectoryOnly).Any();
    }
}
