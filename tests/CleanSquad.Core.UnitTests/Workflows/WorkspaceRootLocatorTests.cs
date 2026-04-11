using System;
using System.IO;
using CleanSquad.Core.Workflows;

namespace CleanSquad.Core.UnitTests.Workflows;

/// <summary>
///     Unit tests for <see cref="WorkspaceRootLocator" />.
/// </summary>
public sealed class WorkspaceRootLocatorTests
{
    /// <summary>
    ///     Verifies the locator prefers a source-control marker instead of language-specific build files.
    /// </summary>
    [Fact]
    public void FindWorkspaceRootUsesSourceControlMarkers()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            string workspaceRootPath = Path.Combine(tempDirectoryPath, "workspace");
            string nestedPath = Path.Combine(workspaceRootPath, "src", "tool");
            Directory.CreateDirectory(nestedPath);
            Directory.CreateDirectory(Path.Combine(workspaceRootPath, ".git"));

            string rootPath = WorkspaceRootLocator.FindWorkspaceRoot(nestedPath);

            Assert.Equal(workspaceRootPath, rootPath);
        }
        finally
        {
            Directory.Delete(tempDirectoryPath, true);
        }
    }

    /// <summary>
    ///     Verifies the locator falls back to the starting directory when no workspace markers exist.
    /// </summary>
    [Fact]
    public void FindWorkspaceRootFallsBackToStartingDirectoryWhenNoMarkersExist()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            string nestedPath = Path.Combine(tempDirectoryPath, "standalone", "tool");
            Directory.CreateDirectory(nestedPath);

            string rootPath = WorkspaceRootLocator.FindWorkspaceRoot(nestedPath);

            Assert.Equal(nestedPath, rootPath);
        }
        finally
        {
            Directory.Delete(tempDirectoryPath, true);
        }
    }

    private static string CreateTempDirectory()
    {
        string tempDirectoryPath = Path.Combine(Path.GetTempPath(), $"clean-squad-root-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectoryPath);
        return tempDirectoryPath;
    }
}
