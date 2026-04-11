using System;
using System.IO;
using CleanSquad.Core.Workflows;

namespace CleanSquad.Core.UnitTests.Workflows;

/// <summary>
///     Unit tests for <see cref="RepositoryRootLocator" />.
/// </summary>
public sealed class RepositoryRootLocatorTests
{
    /// <summary>
    ///     Verifies the locator prefers a generic workspace marker rather than a solution-specific file name.
    /// </summary>
    [Fact]
    public void FindRepositoryRootUsesGenericWorkspaceMarkers()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            string workspaceRootPath = Path.Combine(tempDirectoryPath, "workspace");
            string nestedPath = Path.Combine(workspaceRootPath, "src", "tool");
            Directory.CreateDirectory(nestedPath);
            File.WriteAllText(Path.Combine(workspaceRootPath, "Directory.Build.props"), "<Project />");

            string rootPath = RepositoryRootLocator.FindRepositoryRoot(nestedPath);

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
    public void FindRepositoryRootFallsBackToStartingDirectoryWhenNoMarkersExist()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            string nestedPath = Path.Combine(tempDirectoryPath, "standalone", "tool");
            Directory.CreateDirectory(nestedPath);

            string rootPath = RepositoryRootLocator.FindRepositoryRoot(nestedPath);

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
