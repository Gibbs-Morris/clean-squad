using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CleanSquad.Workflow.Orchestration;

/// <summary>
///     Represents one structured workflow log event.
/// </summary>
[JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
public sealed class WorkflowLogEntry
{
    /// <summary>
    ///     Gets or sets the UTC timestamp for the event.
    /// </summary>
    public DateTimeOffset TimestampUtc { get; set; }

    /// <summary>
    ///     Gets or sets the severity level.
    /// </summary>
    public string Level { get; set; } = "Information";

    /// <summary>
    ///     Gets or sets the event name.
    /// </summary>
    public string EventName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the node identifier associated with the event.
    /// </summary>
    public string? NodeId { get; set; }

    /// <summary>
    ///     Gets or sets the human-readable message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    ///     Gets the optional structured properties.
    /// </summary>
    public Dictionary<string, string> Properties { get; } = [];
}
