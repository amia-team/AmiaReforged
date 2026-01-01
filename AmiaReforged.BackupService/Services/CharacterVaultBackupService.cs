using AmiaReforged.BackupService.Configuration;

namespace AmiaReforged.BackupService.Services;

/// <summary>
/// Service responsible for backing up character vault files.
/// </summary>
public interface ICharacterVaultBackupService
{
    /// <summary>
    /// Copies all character vault files from the source directory to the backup destination.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success/failure and any error message</returns>
    Task<CharacterVaultBackupResult> BackupCharacterVaultAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a character vault backup operation.
/// </summary>
public class CharacterVaultBackupResult
{
    public bool Success { get; init; }
    public int FilesCopied { get; init; }
    public int DirectoriesCopied { get; init; }
    public string? ErrorMessage { get; init; }

    public static CharacterVaultBackupResult Succeeded(int files, int directories) => new()
    {
        Success = true,
        FilesCopied = files,
        DirectoriesCopied = directories
    };

    public static CharacterVaultBackupResult Failed(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };
}

public class CharacterVaultBackupService : ICharacterVaultBackupService
{
    private readonly ILogger<CharacterVaultBackupService> _logger;
    private readonly BackupConfig _config;

    public CharacterVaultBackupService(ILogger<CharacterVaultBackupService> logger, BackupConfig config)
    {
        _logger = logger;
        _config = config;
    }

    public async Task<CharacterVaultBackupResult> BackupCharacterVaultAsync(CancellationToken cancellationToken = default)
    {
        string sourcePath = _config.GetServerVaultSourcePath();
        string destinationPath = Path.Combine(_config.BackupDirectory, _config.CharactersBackupSubdirectory);

        if (string.IsNullOrEmpty(sourcePath))
        {
            string message = "Server vault source path is not configured. Set SERVERVAULT_PATH environment variable or configure ServerVaultSourcePath in appsettings.json";
            _logger.LogWarning(message);
            return CharacterVaultBackupResult.Failed(message);
        }

        if (!Directory.Exists(sourcePath))
        {
            string message = $"Server vault source directory does not exist: {sourcePath}";
            _logger.LogWarning(message);
            return CharacterVaultBackupResult.Failed(message);
        }

        _logger.LogInformation("Starting character vault backup from {Source} to {Destination}", sourcePath, destinationPath);

        try
        {
            // Ensure destination directory exists
            if (!Directory.Exists(destinationPath))
            {
                Directory.CreateDirectory(destinationPath);
                _logger.LogInformation("Created destination directory: {Path}", destinationPath);
            }

            int filesCopied = 0;
            int directoriesCopied = 0;

            await Task.Run(() =>
            {
                CopyDirectoryRecursive(sourcePath, destinationPath, ref filesCopied, ref directoriesCopied, cancellationToken);
            }, cancellationToken);

            _logger.LogInformation("Character vault backup completed. Copied {Files} files in {Directories} directories",
                filesCopied, directoriesCopied);

            return CharacterVaultBackupResult.Succeeded(filesCopied, directoriesCopied);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Character vault backup was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            string message = $"Failed to backup character vault: {ex.Message}";
            _logger.LogError(ex, "Failed to backup character vault from {Source} to {Destination}", sourcePath, destinationPath);
            return CharacterVaultBackupResult.Failed(message);
        }
    }

    private void CopyDirectoryRecursive(string sourceDir, string destinationDir, ref int filesCopied, ref int directoriesCopied, CancellationToken cancellationToken)
    {
        // Get all subdirectories
        string[] directories = Directory.GetDirectories(sourceDir);

        foreach (string directory in directories)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string dirName = Path.GetFileName(directory);
            string destSubDir = Path.Combine(destinationDir, dirName);

            if (!Directory.Exists(destSubDir))
            {
                Directory.CreateDirectory(destSubDir);
            }

            directoriesCopied++;

            // Recursively copy subdirectory contents
            CopyDirectoryRecursive(directory, destSubDir, ref filesCopied, ref directoriesCopied, cancellationToken);
        }

        // Copy all files in current directory
        string[] files = Directory.GetFiles(sourceDir);

        foreach (string file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string fileName = Path.GetFileName(file);
            string destFile = Path.Combine(destinationDir, fileName);

            File.Copy(file, destFile, overwrite: true);
            filesCopied++;

            _logger.LogDebug("Copied: {FileName}", fileName);
        }
    }
}

