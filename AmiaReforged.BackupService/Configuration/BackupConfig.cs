namespace AmiaReforged.BackupService.Configuration;

/// <summary>
/// Configuration for a single database to backup.
/// </summary>
public class DatabaseConfig
{
    public string Name { get; set; } = string.Empty;
    public string HostEnvVar { get; set; } = string.Empty;
    public string PortEnvVar { get; set; } = string.Empty;
    public string DatabaseEnvVar { get; set; } = string.Empty;
    public string UserEnvVar { get; set; } = string.Empty;
    public string PasswordEnvVar { get; set; } = string.Empty;

    // Defaults if env vars not set
    public string DefaultHost { get; set; } = "localhost";
    public int DefaultPort { get; set; } = 5432;
    public string DefaultDatabase { get; set; } = string.Empty;
    public string DefaultUser { get; set; } = "amia";
    public string DefaultPassword { get; set; } = string.Empty;

    public string GetHost() => Environment.GetEnvironmentVariable(HostEnvVar) ?? DefaultHost;
    public int GetPort() => int.TryParse(Environment.GetEnvironmentVariable(PortEnvVar), out int port) ? port : DefaultPort;
    public string GetDatabase() => Environment.GetEnvironmentVariable(DatabaseEnvVar) ?? DefaultDatabase;
    public string GetUser() => Environment.GetEnvironmentVariable(UserEnvVar) ?? DefaultUser;
    public string GetPassword() => Environment.GetEnvironmentVariable(PasswordEnvVar) ?? DefaultPassword;
}

/// <summary>
/// Configuration for the backup service.
/// </summary>
public class BackupConfig
{
    /// <summary>
    /// Directory where SQL backup files are written before git commit.
    /// </summary>
    public string BackupDirectory { get; set; } = "/var/backups/amia";

    /// <summary>
    /// Path to the git repository where backups are committed and pushed.
    /// </summary>
    public string GitRepositoryPath { get; set; } = "/var/backups/amia";

    /// <summary>
    /// Git remote name to push to.
    /// </summary>
    public string GitRemote { get; set; } = "origin";

    /// <summary>
    /// Git branch to push to.
    /// </summary>
    public string GitBranch { get; set; } = "main";

    /// <summary>
    /// Interval in minutes between backups.
    /// </summary>
    public int IntervalMinutes { get; set; } = 60;

    /// <summary>
    /// Path to pg_dump executable. If empty, assumes it's in PATH.
    /// </summary>
    public string PgDumpPath { get; set; } = "pg_dump";

    /// <summary>
    /// List of databases to backup.
    /// </summary>
    public List<DatabaseConfig> Databases { get; set; } = new();

    /// <summary>
    /// Environment variable name for server vault source path.
    /// </summary>
    public string ServerVaultSourcePathEnvVar { get; set; } = "SERVERVAULT_PATH";

    /// <summary>
    /// Default path to the server vault source directory (used if env var not set).
    /// </summary>
    public string DefaultServerVaultSourcePath { get; set; } = string.Empty;

    /// <summary>
    /// Subdirectory under BackupDirectory where character files are copied to.
    /// </summary>
    public string CharactersBackupSubdirectory { get; set; } = "characters";

    /// <summary>
    /// Whether to enable character vault backup.
    /// </summary>
    public bool EnableCharacterVaultBackup { get; set; } = true;

    /// <summary>
    /// Discord webhook URL for error notifications (optional).
    /// </summary>
    public string DiscordWebhookUrlEnvVar { get; set; } = "DISCORD_WEBHOOK_URL";

    /// <summary>
    /// URL for the server health check endpoint.
    /// </summary>
    public string ServerHealthEndpoint { get; set; } = "http://localhost:8080/api/worldengine/health";

    /// <summary>
    /// Environment variable name for server health endpoint URL.
    /// </summary>
    public string ServerHealthEndpointEnvVar { get; set; } = "SERVER_HEALTH_ENDPOINT";

    /// <summary>
    /// Timeout in seconds for server health check.
    /// </summary>
    public int HealthCheckTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Whether to skip character vault backup when server is unhealthy.
    /// </summary>
    public bool SkipCharacterBackupOnUnhealthyServer { get; set; } = true;

    /// <summary>
    /// Gets the server vault source path from environment variable or default.
    /// </summary>
    public string GetServerVaultSourcePath() =>
        Environment.GetEnvironmentVariable(ServerVaultSourcePathEnvVar) ?? DefaultServerVaultSourcePath;

    /// <summary>
    /// Gets the Discord webhook URL from environment variable.
    /// </summary>
    public string? GetDiscordWebhookUrl() =>
        Environment.GetEnvironmentVariable(DiscordWebhookUrlEnvVar);

    /// <summary>
    /// Gets the server health endpoint URL from environment variable or default.
    /// </summary>
    public string GetServerHealthEndpoint() =>
        Environment.GetEnvironmentVariable(ServerHealthEndpointEnvVar) ?? ServerHealthEndpoint;
}
