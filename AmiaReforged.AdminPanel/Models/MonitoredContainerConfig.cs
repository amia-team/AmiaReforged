namespace AmiaReforged.AdminPanel.Models;

/// <summary>
/// Configuration for a monitored container, persisted to JSON.
/// </summary>
public class MonitoredContainerConfig
{
    /// <summary>
    /// Docker container ID (short or full form).
    /// </summary>
    public string ContainerId { get; set; } = string.Empty;

    /// <summary>
    /// Friendly display name for the container.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Whether this container is actively being monitored.
    /// </summary>
    public bool IsMonitoringEnabled { get; set; } = true;

    /// <summary>
    /// Whether to automatically restart on crash detection.
    /// </summary>
    public bool AutoRestartEnabled { get; set; } = false;

    /// <summary>
    /// Pipe-delimited list of regex patterns to watch for (e.g., "segfault|SIGSEGV|core dumped").
    /// </summary>
    public string WatchPatterns { get; set; } = "segfault|SIGSEGV|core dumped";
}

/// <summary>
/// Root object for JSON serialization of monitoring state.
/// </summary>
public class MonitoringState
{
    public List<MonitoredContainerConfig> Containers { get; set; } = new();
}

