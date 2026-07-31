using System.Collections.Generic;

namespace CleanSquad.Core.UnitTests;

/// <summary>
///     Unit tests for <see cref="CleanupChecklistService" />.
/// </summary>
public sealed class CleanupChecklistServiceTests
{
    /// <summary>
    ///     Verifies the starter checklist uses the default name when the input is blank.
    /// </summary>
    [Fact]
    public void CreateStarterChecklistUsesDefaultNameWhenInputIsBlank()
    {
        IReadOnlyList<CleanTask> checklist = CleanupChecklistService.CreateStarterChecklist(" ");

        Assert.Collection(
            checklist,
            first =>
            {
                Assert.Equal("Define the mission for CleanSquad", first.Name);
                Assert.False(first.IsComplete);
            },
            second => Assert.Equal("Keep the build green", second.Name),
            third => Assert.Equal("Ship with confidence", third.Name));
    }

    /// <summary>
    ///     Verifies the summary trims the provided squad name.
    /// </summary>
    [Fact]
    public void CreateSummaryTrimsProvidedSquadName()
    {
        string summary = CleanupChecklistService.CreateSummary("  Night Shift  ");

        Assert.Equal("Night Shift: 3 starter tasks ready.", summary);
    }

    /// <summary>
    ///     Verifies configurable checklist text can supply branded defaults without changing code.
    /// </summary>
    [Fact]
    public void CreateStarterChecklistUsesConfiguredBranding()
    {
        CleanupChecklistOptions options = new()
        {
            DefaultSquadName = "Acme SDLC",
            MissionTaskTemplate = "Codify the mission for {SquadName}",
            BuildTaskName = "Protect the golden pipeline",
            ReleaseTaskName = "Ship the package with confidence",
            SummaryTemplate = "{SquadName}: {TaskCount} workflow policies ready.",
        };

        IReadOnlyList<CleanTask> checklist = CleanupChecklistService.CreateStarterChecklist(null, options);
        string summary = CleanupChecklistService.CreateSummary(null, options);

        Assert.Collection(
            checklist,
            first => Assert.Equal("Codify the mission for Acme SDLC", first.Name),
            second => Assert.Equal("Protect the golden pipeline", second.Name),
            third => Assert.Equal("Ship the package with confidence", third.Name));
        Assert.Equal("Acme SDLC: 3 workflow policies ready.", summary);
    }
}
