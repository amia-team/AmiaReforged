namespace WorldSimulator.Domain.ValueObjects
{
    /// <summary>
    /// Severity level for events and notifications
    /// </summary>
    public enum EventSeverity
    {
        /// <summary>Informational event</summary>
        Information = 0,

        /// <summary>Warning that requires attention</summary>
        Warning = 1,

        /// <summary>Error that caused failure</summary>
        Error = 2,

        /// <summary>Critical system failure</summary>
        Critical = 3
    }
}

