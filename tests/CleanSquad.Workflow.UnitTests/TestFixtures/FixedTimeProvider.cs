using System;

namespace CleanSquad.Workflow.UnitTests.TestFixtures;

/// <summary>
///     A deterministic time provider that always returns the configured UTC timestamp.
/// </summary>
internal sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset utcNow;

    /// <summary>
    ///     Initializes a new instance of the <see cref="FixedTimeProvider" /> class.
    /// </summary>
    /// <param name="utcNow">The fixed UTC timestamp to return.</param>
    public FixedTimeProvider(DateTimeOffset utcNow)
    {
        this.utcNow = utcNow;
    }

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow()
    {
        return this.utcNow;
    }
}
