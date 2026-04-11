using System;
using System.IO;
using System.Linq;

namespace CleanSquad.Core.Workflows;

/// <summary>
///     Locates the most appropriate workspace root by walking up the directory tree.
/// </summary>
public static class WorkspaceRootLocator
{
    private static readonly string[] SourceControlMarkers = [".git", ".hg", ".jj", ".svn"];

    /// <summary>
    ///     Finds the most appropriate workspace root from a starting path.
    /// </summary>
    /// <param name="startPath">The starting directory or file path.</param>
    /// <returns>The located workspace root path, or the starting directory when no markers are found.</returns>
    public static string FindWorkspaceRoot(string startPath)
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
        return SourceControlMarkers.Any(marker => Directory.Exists(Path.Combine(directory.FullName, marker)));
    }
}
