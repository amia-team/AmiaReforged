namespace AmiaReforged.AdminPanel.Configuration;

/// <summary>
/// A named server directory that area files can be uploaded to.
/// Configured via AREA_UPLOAD_PATH_* environment variables using the format "DisplayName=/absolute/path".
/// </summary>
public sealed record AreaUploadTarget(string Name, string Path);

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
    /// Dev account username (read-only access, log streaming only).
    /// </summary>
    public string DevUsername { get; set; } = "dev";

    /// <summary>
    /// Dev account password (should be overridden via environment variable).
    /// </summary>
    public string DevPassword { get; set; } = "DevPass123!";

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

    /// <summary>
    /// Named server directories that area files (.are, .git, .gic) can be uploaded to.
    /// Populated from AREA_UPLOAD_PATH_* environment variables.
    /// </summary>
    public List<AreaUploadTarget> AreaUploadTargets { get; set; } = new();
}
