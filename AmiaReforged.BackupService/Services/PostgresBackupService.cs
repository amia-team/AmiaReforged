using System.Diagnostics;
using AmiaReforged.BackupService.Configuration;

namespace AmiaReforged.BackupService.Services;

/// <summary>
/// Service responsible for creating PostgreSQL database backups using pg_dump.
/// </summary>
public interface IPostgresBackupService
{
    /// <summary>
    /// Creates a SQL backup of the specified database.
    /// </summary>
    /// <param name="dbConfig">Database configuration</param>
    /// <param name="outputPath">Full path for the output SQL file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if backup succeeded, false otherwise</returns>
    Task<bool> BackupDatabaseAsync(DatabaseConfig dbConfig, string outputPath, CancellationToken cancellationToken = default);
}

public class PostgresBackupService : IPostgresBackupService
{
    private readonly ILogger<PostgresBackupService> _logger;
    private readonly BackupConfig _config;

    public PostgresBackupService(ILogger<PostgresBackupService> logger, BackupConfig config)
    {
        _logger = logger;
        _config = config;
    }

    public async Task<bool> BackupDatabaseAsync(DatabaseConfig dbConfig, string outputPath, CancellationToken cancellationToken = default)
    {
        string host = dbConfig.GetHost();
        int port = dbConfig.GetPort();
        string database = dbConfig.GetDatabase();
        string user = dbConfig.GetUser();
        string password = dbConfig.GetPassword();

        if (string.IsNullOrEmpty(database))
        {
            _logger.LogError("Database name is empty for {ConfigName}", dbConfig.Name);
            return false;
        }

        _logger.LogInformation("Starting backup of database {Database} on {Host}:{Port}", database, host, port);

        // Ensure output directory exists
        string? outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        try
        {
            ProcessStartInfo psi = new()
            {
                FileName = _config.PgDumpPath,
                Arguments = BuildPgDumpArguments(host, port, database, user, outputPath),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Set PGPASSWORD environment variable for authentication
            psi.Environment["PGPASSWORD"] = password;

            using Process? process = Process.Start(psi);
            if (process == null)
            {
                _logger.LogError("Failed to start pg_dump process");
                return false;
            }

            string stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            string stderr = await process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                _logger.LogError("pg_dump failed with exit code {ExitCode}. Error: {Error}", 
                    process.ExitCode, stderr);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(stderr))
            {
                // pg_dump sometimes writes informational messages to stderr
                _logger.LogDebug("pg_dump stderr: {Stderr}", stderr);
            }

            FileInfo fileInfo = new(outputPath);
            _logger.LogInformation("Backup of {Database} completed successfully. File size: {Size} bytes", 
                database, fileInfo.Length);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during backup of {Database}", database);
            return false;
        }
    }

    private static string BuildPgDumpArguments(string host, int port, string database, string user, string outputPath)
    {
        // Use plain SQL format for easy git diffs
        // --no-owner and --no-acl make the dump more portable
        // --clean adds DROP statements before CREATE for easier restoration
        return $"-h {host} -p {port} -U {user} --format=plain --no-owner --no-acl --clean -f \"{outputPath}\" {database}";
    }
}
