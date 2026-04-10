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
}
