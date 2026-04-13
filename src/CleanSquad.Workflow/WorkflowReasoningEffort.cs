using System;
using System.Collections.Generic;
using System.Linq;

namespace CleanSquad.Workflow;

/// <summary>
///     Provides the supported reasoning-effort values for workflow-backed Copilot sessions.
/// </summary>
public static class WorkflowReasoningEffort
{
    /// <summary>
    ///     Requests low reasoning effort.
    /// </summary>
    public const string Low = "low";

    /// <summary>
    ///     Requests medium reasoning effort.
    /// </summary>
    public const string Medium = "medium";

    /// <summary>
    ///     Requests high reasoning effort.
    /// </summary>
    public const string High = "high";

    /// <summary>
    ///     Requests extra-high reasoning effort.
    /// </summary>
    public const string ExtraHigh = "xhigh";

    /// <summary>
    ///     Requests the highest reasoning effort supported by the selected model.
    /// </summary>
    public const string HighestSupported = "highest-supported";

    private static readonly Dictionary<string, int> EffortRankings = new(StringComparer.OrdinalIgnoreCase)
    {
        [Low] = 0,
        [Medium] = 1,
        [High] = 2,
        [ExtraHigh] = 3,
    };

    /// <summary>
    ///     Normalizes a configured reasoning-effort value to the canonical workflow value.
    /// </summary>
    /// <param name="value">The configured reasoning-effort value.</param>
    /// <returns>The canonical value, or <see langword="null" /> when the value is blank or unsupported.</returns>
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string trimmedValue = value.Trim();
        if (trimmedValue.Equals(HighestSupported, StringComparison.OrdinalIgnoreCase)
            || trimmedValue.Equals("highest", StringComparison.OrdinalIgnoreCase)
            || trimmedValue.Equals("max", StringComparison.OrdinalIgnoreCase)
            || trimmedValue.Equals("maximum", StringComparison.OrdinalIgnoreCase)
            || trimmedValue.Equals("max-supported", StringComparison.OrdinalIgnoreCase)
            || trimmedValue.Equals("maximum-supported", StringComparison.OrdinalIgnoreCase))
        {
            return HighestSupported;
        }

        if (trimmedValue.Equals(Low, StringComparison.OrdinalIgnoreCase))
        {
            return Low;
        }

        if (trimmedValue.Equals(Medium, StringComparison.OrdinalIgnoreCase))
        {
            return Medium;
        }

        if (trimmedValue.Equals(High, StringComparison.OrdinalIgnoreCase))
        {
            return High;
        }

        return trimmedValue.Equals(ExtraHigh, StringComparison.OrdinalIgnoreCase) ? ExtraHigh : null;
    }

    /// <summary>
    ///     Determines whether the configured value requests the highest model-supported effort.
    /// </summary>
    /// <param name="value">The configured reasoning-effort value.</param>
    /// <returns><see langword="true" /> when the value means highest supported; otherwise, <see langword="false" />.</returns>
    public static bool IsHighestSupportedValue(string? value)
    {
        return string.Equals(Normalize(value), HighestSupported, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Determines whether a configured value is supported by the workflow model.
    /// </summary>
    /// <param name="value">The configured reasoning-effort value.</param>
    /// <returns><see langword="true" /> when the value is supported; otherwise, <see langword="false" />.</returns>
    public static bool IsSupportedConfiguration(string? value)
    {
        return Normalize(value) is not null;
    }

    /// <summary>
    ///     Selects the highest supported reasoning-effort value from the provided model metadata.
    /// </summary>
    /// <param name="supportedReasoningEfforts">The supported reasoning-effort values reported by the model metadata.</param>
    /// <returns>The highest supported reasoning-effort value, or <see langword="null" /> when none are supported.</returns>
    public static string? SelectHighestSupported(IReadOnlyList<string>? supportedReasoningEfforts)
    {
        if (supportedReasoningEfforts is null || supportedReasoningEfforts.Count == 0)
        {
            return null;
        }

        List<string> normalizedValues = supportedReasoningEfforts
            .Select(Normalize)
            .Where(static value => value is not null && !string.Equals(value, HighestSupported, StringComparison.Ordinal))
            .Select(static value => value!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return normalizedValues.Count == 0
            ? null
            : normalizedValues.OrderByDescending(GetRank).First();
    }

    private static int GetRank(string value)
    {
        return EffortRankings.TryGetValue(value, out int rank)
            ? rank
            : int.MinValue;
    }
}
