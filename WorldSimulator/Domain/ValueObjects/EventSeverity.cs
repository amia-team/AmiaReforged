namespace WorldSimulator.Domain.ValueObjects;

/// <summary>
/// Event severity levels for logging and Discord notifications
/// </summary>
public enum EventSeverity
{
    /// <summary>Informational events for normal operation</summary>
    Info = 0,

    /// <summary>Warning events that may require attention</summary>
    Warning = 1,

    /// <summary>Critical events requiring immediate attention</summary>
    Critical = 2
}

