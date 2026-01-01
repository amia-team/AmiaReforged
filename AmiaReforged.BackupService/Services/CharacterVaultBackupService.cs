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
    public int FilesSkipped { get; init; }
    public string? ErrorMessage { get; init; }
    public List<string> Warnings { get; init; } = new();

    public static CharacterVaultBackupResult Succeeded(int files, int directories, int skipped = 0, List<string>? warnings = null) => new()
    {
        Success = true,
        FilesCopied = files,
        DirectoriesCopied = directories,
        FilesSkipped = skipped,
        Warnings = warnings ?? new List<string>()
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
            int filesSkipped = 0;
            List<string> warnings = new();

            await Task.Run(() =>
            {
                CopyDirectoryRecursive(sourcePath, destinationPath, ref filesCopied, ref directoriesCopied, ref filesSkipped, warnings, cancellationToken);
            }, cancellationToken);

            if (filesSkipped > 0)
            {
                _logger.LogWarning("Character vault backup completed with warnings. Copied {Files} files in {Directories} directories, skipped {Skipped} files",
                    filesCopied, directoriesCopied, filesSkipped);
            }
            else
            {
                _logger.LogInformation("Character vault backup completed. Copied {Files} files in {Directories} directories",
                    filesCopied, directoriesCopied);
            }

            return CharacterVaultBackupResult.Succeeded(filesCopied, directoriesCopied, filesSkipped, warnings);
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

    private void CopyDirectoryRecursive(string sourceDir, string destinationDir, ref int filesCopied, ref int directoriesCopied, ref int filesSkipped, List<string> warnings, CancellationToken cancellationToken)
    {
        // Get all subdirectories
        string[] directories;
        try
        {
            directories = Directory.GetDirectories(sourceDir);
        }
        catch (Exception ex)
        {
            string warning = $"Failed to enumerate directories in {sourceDir}: {ex.Message}";
            _logger.LogWarning(ex, "Failed to enumerate directories in {SourceDir}", sourceDir);
            warnings.Add(warning);
            return;
        }

        foreach (string directory in directories)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string dirName = Path.GetFileName(directory);
            string destSubDir = Path.Combine(destinationDir, dirName);

            try
            {
                if (!Directory.Exists(destSubDir))
                {
                    Directory.CreateDirectory(destSubDir);
                }

                directoriesCopied++;

                // Recursively copy subdirectory contents
                CopyDirectoryRecursive(directory, destSubDir, ref filesCopied, ref directoriesCopied, ref filesSkipped, warnings, cancellationToken);
            }
            catch (Exception ex)
            {
                string warning = $"Failed to process directory {dirName}: {ex.Message}";
                _logger.LogWarning(ex, "Failed to process directory {DirName}", dirName);
                warnings.Add(warning);
            }
        }

        // Copy all files in current directory
        string[] files;
        try
        {
            files = Directory.GetFiles(sourceDir);
        }
        catch (Exception ex)
        {
            string warning = $"Failed to enumerate files in {sourceDir}: {ex.Message}";
            _logger.LogWarning(ex, "Failed to enumerate files in {SourceDir}", sourceDir);
            warnings.Add(warning);
            return;
        }

        foreach (string file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string fileName = Path.GetFileName(file);
            string destFile = Path.Combine(destinationDir, fileName);

            try
            {
                File.Copy(file, destFile, overwrite: true);
                filesCopied++;
                _logger.LogDebug("Copied: {FileName}", fileName);
            }
            catch (Exception ex)
            {
                // Try fallback to native cp command for problematic files
                _logger.LogDebug("File.Copy failed for {FileName}, attempting cp fallback: {Error}", fileName, ex.Message);

                if (TryCopyWithNativeCommand(file, destFile))
                {
                    filesCopied++;
                    _logger.LogDebug("Copied with cp fallback: {FileName}", fileName);
                }
                else
                {
                    filesSkipped++;
                    string warning = $"Failed to copy file {fileName}: {ex.Message}";
                    _logger.LogWarning("Skipped file {File}: {Error}", file, ex.Message);
                    warnings.Add(warning);
                }
            }
        }
    }

    /// <summary>
    /// Attempts to copy a file using the native cp command as a fallback for problematic filenames.
    /// </summary>
    private bool TryCopyWithNativeCommand(string source, string destination)
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cp",
                    Arguments = $"-f \"{source}\" \"{destination}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit(TimeSpan.FromSeconds(30));

            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Native cp fallback failed: {Error}", ex.Message);
            return false;
        }
    }
}

