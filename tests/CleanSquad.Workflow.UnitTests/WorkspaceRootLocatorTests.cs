using System;
using System.IO;
using CleanSquad.Workflow.Infrastructure;

namespace CleanSquad.Workflow.UnitTests;

/// <summary>
///     Unit tests for <see cref="WorkspaceRootLocator" />.
/// </summary>
public sealed class WorkspaceRootLocatorTests
{
    /// <summary>
    ///     Verifies the locator prefers source-control markers.
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

    private static string CreateTempDirectory()
    {
        string tempDirectoryPath = Path.Combine(Path.GetTempPath(), $"clean-squad-root-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectoryPath);
        return tempDirectoryPath;
    }
}
