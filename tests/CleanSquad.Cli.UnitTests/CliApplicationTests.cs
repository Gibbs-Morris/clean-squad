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
}
