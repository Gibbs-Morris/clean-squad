using System;
using System.Globalization;

namespace CleanSquad.Workflow;

/// <summary>
///     Normalizes and parses workflow response-timeout settings.
/// </summary>
public static class WorkflowResponseTimeout
{
    /// <summary>
    ///     Normalizes a configured timeout string.
    /// </summary>
    /// <param name="value">The configured timeout value.</param>
    /// <returns>The trimmed timeout value, or <see langword="null" /> when no value is configured.</returns>
    public static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    ///     Attempts to parse a configured timeout string using the .NET <see cref="TimeSpan" /> format.
    /// </summary>
    /// <param name="value">The configured timeout value.</param>
    /// <param name="timeout">The parsed timeout when successful.</param>
    /// <returns><see langword="true" /> when the timeout is valid and positive; otherwise, <see langword="false" />.</returns>
    public static bool TryParse(string? value, out TimeSpan timeout)
    {
        string? normalizedValue = Normalize(value);
        if (!string.IsNullOrWhiteSpace(normalizedValue)
            && TimeSpan.TryParse(normalizedValue, CultureInfo.InvariantCulture, out timeout)
            && timeout > TimeSpan.Zero)
        {
            return true;
        }

        timeout = default;
        return false;
    }
}
