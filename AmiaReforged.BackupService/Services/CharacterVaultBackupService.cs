using System.Text;
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

    public static CharacterVaultBackupResult Succeeded(int files, int directories, int skipped = 0,
        List<string>? warnings = null) => new()
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

    public async Task<CharacterVaultBackupResult> BackupCharacterVaultAsync(
        CancellationToken cancellationToken = default)
    {
        string sourcePath = _config.GetServerVaultSourcePath();
        string destinationPath = Path.Combine(_config.BackupDirectory, _config.CharactersBackupSubdirectory);

        if (string.IsNullOrEmpty(sourcePath))
        {
            string message =
                "Server vault source path is not configured. Set SERVERVAULT_PATH environment variable or configure ServerVaultSourcePath in appsettings.json";
            _logger.LogWarning(message);
            return CharacterVaultBackupResult.Failed(message);
        }

        if (!Directory.Exists(sourcePath))
        {
            string message = $"Server vault source directory does not exist: {sourcePath}";
            _logger.LogWarning(message);
            return CharacterVaultBackupResult.Failed(message);
        }

        _logger.LogInformation("Starting character vault backup from {Source} to {Destination}", sourcePath,
            destinationPath);

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

            await Task.Run(
                () =>
                {
                    CopyDirectoryRecursive(sourcePath, destinationPath, ref filesCopied, ref directoriesCopied,
                        ref filesSkipped, warnings, cancellationToken);
                }, cancellationToken);

            if (filesSkipped > 0)
            {
                _logger.LogWarning(
                    "Character vault backup completed with warnings. Copied {Files} files in {Directories} directories, skipped {Skipped} files",
                    filesCopied, directoriesCopied, filesSkipped);
            }
            else
            {
                _logger.LogInformation(
                    "Character vault backup completed. Copied {Files} files in {Directories} directories",
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
            _logger.LogError(ex, "Failed to backup character vault from {Source} to {Destination}", sourcePath,
                destinationPath);
            return CharacterVaultBackupResult.Failed(message);
        }
    }

    private void CopyDirectoryRecursive(string sourceDir, string destinationDir, ref int filesCopied,
        ref int directoriesCopied, ref int filesSkipped, List<string> warnings, CancellationToken cancellationToken)
    {
        // Get all subdirectories
        string[] directories;
        try
        {
            directories = Directory.GetDirectories(sourceDir);
        }
        catch (Exception ex)
        {
            string dirEnumWarning = $"Failed to enumerate directories in {sourceDir}: {ex.Message}";
            _logger.LogWarning(ex, "Failed to enumerate directories in {SourceDir}", sourceDir);
            warnings.Add(dirEnumWarning);
            return;
        }

        foreach (string directory in directories)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string dirName = Path.GetFileName(directory);
            string sanitizedDirName = SanitizeFileName(dirName);
            string destSubDir = Path.Combine(destinationDir, sanitizedDirName);

            try
            {
                if (!Directory.Exists(destSubDir))
                {
                    Directory.CreateDirectory(destSubDir);
                }

                directoriesCopied++;

                // Recursively copy subdirectory contents
                CopyDirectoryRecursive(directory, destSubDir, ref filesCopied, ref directoriesCopied, ref filesSkipped,
                    warnings, cancellationToken);
            }
            catch (Exception ex)
            {
                string dirProcessWarning = $"Failed to process directory {dirName}: {ex.Message}";
                _logger.LogWarning(ex, "Failed to process directory {DirName}", dirName);
                warnings.Add(dirProcessWarning);
            }
        }

        // Copy files - use native shell to enumerate and copy files with problematic names
        var (success, copied, skipped, copyWarning) = CopyFilesWithSanitizedNames(sourceDir, destinationDir);

        filesCopied += copied;
        filesSkipped += skipped;

        if (!string.IsNullOrEmpty(copyWarning))
        {
            warnings.Add(copyWarning);
        }
    }

    /// <summary>
    /// Copies all files from source to destination using native shell to handle Unicode filenames,
    /// sanitizing the destination filenames to ASCII-only.
    /// </summary>
    private (bool Success, int Copied, int Skipped, string? Warning) CopyFilesWithSanitizedNames(string sourceDir,
        string destinationDir)
    {
        try
        {
            // Simple approach: use rsync to copy files, which handles Unicode properly
            // rsync -a copies all files preserving attributes
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c 'cp -f \"{sourceDir}\"/* \"{destinationDir}/\" 2>&1; echo EXIT:$?'",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd().Trim();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit(TimeSpan.FromSeconds(120));

            // Log the raw output for debugging
            _logger.LogDebug("cp output for {Dir}: {Output}", sourceDir, output);
            if (!string.IsNullOrEmpty(error))
            {
                _logger.LogDebug("cp stderr for {Dir}: {Error}", sourceDir, error);
            }

            // Count files in destination directory using ls
            var countProcess = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c 'ls -1 \"{destinationDir}\" 2>/dev/null | wc -l'",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            countProcess.Start();
            string countOutput = countProcess.StandardOutput.ReadToEnd().Trim();
            countProcess.WaitForExit(TimeSpan.FromSeconds(10));

            int.TryParse(countOutput, out int fileCount);

            // Check if cp succeeded (look for EXIT:0 in output)
            bool success = output.Contains("EXIT:0");

            if (!success)
            {
                _logger.LogWarning("cp may have had issues in {Dir}: {Output}", sourceDir, output);
            }

            return (true, fileCount, 0, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy files from {SourceDir}", sourceDir);
            return (false, 0, 0, $"Failed to copy files from {sourceDir}: {ex.Message}");
        }
    }

    /// <summary>
    /// Escapes a string for safe use in a bash single-quoted argument.
    /// Replaces ' with '\'' (end quote, escaped quote, start quote).
    /// </summary>
    private static string EscapeForBash(string path)
    {
        return "'" + path.Replace("'", "'\\''") + "'";
    }

    /// <summary>
    /// Sanitizes a filename by replacing non-ASCII characters with underscores.
    /// Preserves the filename length by using single-character replacement.
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        var sb = new StringBuilder(fileName.Length);
        foreach (char c in fileName)
        {
            // Keep ASCII printable characters (32-126), except problematic ones for filesystems
            if (c >= 32 && c < 127 && c != '<' && c != '>' && c != ':' && c != '"' && c != '|' && c != '?' && c != '*')
            {
                sb.Append(c);
            }
            else
            {
                sb.Append('_'); // Replace with underscore to preserve length
            }
        }

        return sb.ToString();
    }
}
