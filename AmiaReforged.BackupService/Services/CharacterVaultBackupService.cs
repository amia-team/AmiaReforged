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
        // First, try to copy files using .NET, tracking failures
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

        bool hasFailures = false;
        int localFilesCopied = 0;

        foreach (string file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string fileName = Path.GetFileName(file);
            string destFile = Path.Combine(destinationDir, fileName);

            try
            {
                File.Copy(file, destFile, overwrite: true);
                localFilesCopied++;
                _logger.LogDebug("Copied: {FileName}", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogDebug("File.Copy failed for {FileName}: {Error}", fileName, ex.Message);
                hasFailures = true;
            }
        }

        // If any files failed, fall back to native cp for the entire directory contents
        // This bypasses .NET's problematic filename encoding
        if (hasFailures)
        {
            _logger.LogDebug("Some files failed to copy in {Dir}, falling back to native cp", sourceDir);

            var (success, nativeCopied, nativeWarning) = TryCopyDirectoryContentsWithNativeCommand(sourceDir, destinationDir);

            if (success)
            {
                // Native cp copied all files (including ones .NET already copied - it overwrites)
                filesCopied += nativeCopied;
                _logger.LogDebug("Native cp copied {Count} files from {Dir}", nativeCopied, sourceDir);
            }
            else
            {
                // Native cp also failed - count the ones .NET managed to copy
                filesCopied += localFilesCopied;
                filesSkipped += files.Length - localFilesCopied;
                if (!string.IsNullOrEmpty(nativeWarning))
                {
                    warnings.Add(nativeWarning);
                }
                _logger.LogWarning("Native cp fallback failed for {Dir}: {Warning}", sourceDir, nativeWarning);
            }
        }
        else
        {
            filesCopied += localFilesCopied;
        }
    }

    /// <summary>
    /// Attempts to copy all files in a directory using native cp command.
    /// This bypasses .NET's filename encoding issues by letting the shell handle filenames directly.
    /// </summary>
    /// <returns>Tuple of (success, fileCount, warningMessage)</returns>
    private (bool Success, int FileCount, string? Warning) TryCopyDirectoryContentsWithNativeCommand(string sourceDir, string destinationDir)
    {
        try
        {
            // Use cp with shell globbing to copy all files
            // The -f flag forces overwrite, * matches all files
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"cp -f '{sourceDir}'/* '{destinationDir}/' 2>/dev/null || true\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit(TimeSpan.FromSeconds(60));

            // Count files in destination to report how many were copied
            int fileCount = 0;
            try
            {
                // Use ls to count files since .NET might have trouble with some filenames
                var countProcess = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "/bin/bash",
                        Arguments = $"-c \"ls -1 '{destinationDir}' 2>/dev/null | wc -l\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                countProcess.Start();
                string countOutput = countProcess.StandardOutput.ReadToEnd().Trim();
                countProcess.WaitForExit(TimeSpan.FromSeconds(10));
                int.TryParse(countOutput, out fileCount);
            }
            catch
            {
                // If counting fails, just return 0
            }

            // cp with || true always returns 0, check if there were errors
            if (!string.IsNullOrWhiteSpace(stderr))
            {
                return (false, 0, $"Native cp had errors in {sourceDir}: {stderr}");
            }

            return (true, fileCount, null);
        }
        catch (Exception ex)
        {
            return (false, 0, $"Native cp command failed for {sourceDir}: {ex.Message}");
        }
    }
}

