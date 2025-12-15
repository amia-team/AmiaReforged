using AmiaReforged.BackupService.Configuration;
using AmiaReforged.BackupService.Services;

namespace AmiaReforged.BackupService.Application;

/// <summary>
/// Background worker that performs hourly database backups.
/// </summary>
public class BackupWorker : BackgroundService
{
    private readonly ILogger<BackupWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IPostgresBackupService _backupService;
    private readonly IGitBackupService _gitService;
    private readonly BackupConfig _backupConfig;

    public BackupWorker(
        ILogger<BackupWorker> logger,
        IConfiguration configuration,
        IPostgresBackupService backupService,
        IGitBackupService gitService,
        BackupConfig backupConfig)
    {
        _logger = logger;
        _configuration = configuration;
        _backupService = backupService;
        _gitService = gitService;
        _backupConfig = backupConfig;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Backup Worker starting");
        _logger.LogInformation("Backup interval: {Interval} minutes", _backupConfig.IntervalMinutes);
        _logger.LogInformation("Backup directory: {Directory}", _backupConfig.BackupDirectory);
        _logger.LogInformation("Git repository: {Repository}", _backupConfig.GitRepositoryPath);
        _logger.LogInformation("Databases to backup: {Count}", _backupConfig.Databases.Count);

        foreach (DatabaseConfig db in _backupConfig.Databases)
        {
            _logger.LogInformation("  - {Name}: {Database} on {Host}:{Port}", 
                db.Name, db.GetDatabase(), db.GetHost(), db.GetPort());
        }

        // Run initial backup immediately on startup
        await RunBackupCycleAsync(stoppingToken);

        // Then run on schedule
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                TimeSpan delay = TimeSpan.FromMinutes(_backupConfig.IntervalMinutes);
                _logger.LogInformation("Next backup scheduled in {Minutes} minutes", _backupConfig.IntervalMinutes);
                
                await Task.Delay(delay, stoppingToken);
                await RunBackupCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Backup Worker stopping gracefully");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in backup worker loop");
                // Wait a bit before retrying to avoid tight error loops
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Backup Worker stopped");
    }

    private async Task RunBackupCycleAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting backup cycle at {Time}", DateTime.UtcNow);
        
        DateTime backupTime = DateTime.UtcNow;
        string timestamp = backupTime.ToString("yyyy-MM-dd_HH-mm-ss");
        int successCount = 0;
        int failCount = 0;

        foreach (DatabaseConfig dbConfig in _backupConfig.Databases)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            string fileName = $"{dbConfig.Name}_{timestamp}.sql";
            string outputPath = Path.Combine(_backupConfig.BackupDirectory, fileName);

            try
            {
                bool success = await _backupService.BackupDatabaseAsync(dbConfig, outputPath, cancellationToken);
                
                if (success)
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception backing up {Database}", dbConfig.Name);
                failCount++;
            }
        }

        _logger.LogInformation("Backup phase complete. Success: {Success}, Failed: {Failed}", successCount, failCount);

        if (successCount > 0)
        {
            // Commit and push to git
            _logger.LogInformation("Committing backups to git");
            bool pushSuccess = await _gitService.CommitAndPushAsync(cancellationToken);
            
            if (pushSuccess)
            {
                _logger.LogInformation("Backup cycle completed successfully");
            }
            else
            {
                _logger.LogWarning("Backup files created but git push failed");
            }
        }
        else
        {
            _logger.LogWarning("No successful backups to commit");
        }
    }
}
