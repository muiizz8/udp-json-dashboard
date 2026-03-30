using System;

namespace ChatApplication.Common;

/// <summary>
/// Represents a sent or received JSON message for UI logging.
/// </summary>
public sealed class LogEntry
{
    /// <summary>UTC timestamp for the event.</summary>
    public DateTime TimeStampUtc { get; set; }
    /// <summary>Direction: RX, TX, or DBG.</summary>
    public string Direction { get; set; } = "RX";
    /// <summary>Remote endpoint information.</summary>
    public string Remote { get; set; } = "";
    /// <summary>Raw JSON or debug message text.</summary>
    public string Json { get; set; } = "";
}
