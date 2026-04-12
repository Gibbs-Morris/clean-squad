using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CleanSquad.Cli.UnitTests;

/// <summary>
///     Unit tests for <see cref="CliApplication" />.
/// </summary>
public sealed class CliApplicationTests
{
    /// <summary>
    ///     Verifies the CLI uses the default squad name when no arguments are provided.
    /// </summary>
    [Fact]
    public void BuildOutputUsesDefaultSquadNameWhenArgumentsAreMissing()
    {
        string output = CliApplication.BuildOutput([]);

        Assert.Equal("CleanSquad: 3 starter tasks ready.", output);
    }

    /// <summary>
    ///     Verifies the CLI forwards the first argument to the core service.
    /// </summary>
    [Fact]
    public void BuildOutputUsesFirstArgumentAsSquadName()
    {
        string output = CliApplication.BuildOutput(["Delta"]);

        Assert.Equal("Delta: 3 starter tasks ready.", output);
    }

    /// <summary>
    ///     Verifies the CLI can load branded summary text from a local configuration file.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task InvokeAsyncUsesBrandingConfigurationWhenPresentAsync()
    {
        string tempDirectoryPath = Path.Combine(Path.GetTempPath(), $"clean-squad-cli-branding-{System.Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectoryPath);

        try
        {
            string configurationPath = Path.Combine(tempDirectoryPath, "clean-squad.cli.json");
            string configurationJson = string.Join(
                System.Environment.NewLine,
                "{",
                "  \"applicationName\": \"Acme Workflow Kit\",",
                "  \"applicationDescription\": \"Acme Workflow Kit command-line interface.\",",
                "  \"workflowCommandDescription\": \"Run branded workflow packages.\",",
                "  \"checklist\": {",
                "    \"defaultSquadName\": \"Acme SDLC\",",
                "    \"summaryTemplate\": \"{SquadName}: {TaskCount} ready-to-run tasks.\"",
                "  }",
                "}");
            await File.WriteAllTextAsync(configurationPath, configurationJson, Encoding.UTF8);
            using StringWriter outputWriter = new();

            int exitCode = await CliApplication.InvokeAsync([], currentDirectory: tempDirectoryPath, outputWriter: outputWriter);

            Assert.Equal(0, exitCode);
            Assert.Equal("Acme SDLC: 3 ready-to-run tasks.", outputWriter.ToString().Trim());
        }
        finally
        {
            Directory.Delete(tempDirectoryPath, true);
        }
    }
}
