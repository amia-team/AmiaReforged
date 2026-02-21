namespace AmiaReforged.AdminPanel.Configuration;

/// <summary>
/// Configuration settings for the Admin Panel.
/// </summary>
public class AdminPanelConfig
{
    /// <summary>
    /// Default admin username for login.
    /// </summary>
    public string DefaultAdminUsername { get; set; } = "admin";

    /// <summary>
    /// Default admin password (should be overridden via environment variable).
    /// </summary>
    public string DefaultAdminPassword { get; set; } = "ChangeMe123!";

    /// <summary>
    /// Path to the Docker socket.
    /// </summary>
    public string DockerSocketPath { get; set; } = "unix:///var/run/docker.sock";

    /// <summary>
    /// Path to the JSON config file for monitoring state persistence.
    /// </summary>
    public string ConfigPath { get; set; } = "/data/monitoring-config.json";

    /// <summary>
    /// How often to poll container status (in seconds).
    /// </summary>
    public int ContainerPollIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Discord webhook URL for crash notifications (optional).
    /// </summary>
    public string? DiscordWebhookUrl { get; set; }

    /// <summary>
    /// Maximum number of log lines to keep in memory per container.
    /// </summary>
    public int MaxLogLinesPerContainer { get; set; } = 1000;

    /// <summary>
    /// Cooldown period (in seconds) before allowing another auto-restart of the same container.
    /// </summary>
    public int AutoRestartCooldownSeconds { get; set; } = 60;
}
