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
            // Use a bash script that does everything natively to avoid .NET encoding issues
            // The script:
            // 1. Iterates over all files in the source directory
            // 2. Sanitizes each filename (replaces non-ASCII with underscore)
            // 3. Copies with the sanitized destination name
            // 4. Counts successes and failures
            string bashScript = @"
cd ""$1"" || exit 1
copied=0
skipped=0
for file in *; do
    [ -f ""$file"" ] || continue
    # Sanitize filename: replace non-ASCII and problematic chars with underscore
    sanitized=$(echo ""$file"" | sed 's/[^a-zA-Z0-9._-]/_/g')
    if cp -f ""$file"" ""$2/$sanitized"" 2>/dev/null; then
        ((copied++))
    else
        ((skipped++))
    fi
done
echo ""$copied $skipped""
";

            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    // With -c, the first arg after script becomes $0, subsequent become $1, $2...
                    // So we use: -c 'script' _ srcDir destDir (where _ is a placeholder for $0)
                    Arguments =
                        $"-c {EscapeForBash(bashScript)} _ {EscapeForBash(sourceDir)} {EscapeForBash(destinationDir)}",
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

            if (process.ExitCode != 0)
            {
                return (false, 0, 0, $"Bash copy script failed in {sourceDir}: {error}");
            }

            // Parse the output "copied skipped"
            string[] parts = output.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int copied = 0;
            int skipped = 0;

            if (parts.Length >= 1) int.TryParse(parts[0], out copied);
            if (parts.Length >= 2) int.TryParse(parts[1], out skipped);

            if (skipped > 0)
            {
                _logger.LogDebug("Directory {Dir}: copied {Copied}, skipped {Skipped}", sourceDir, copied, skipped);
            }

            return (true, copied, skipped, null);
        }
        catch (Exception ex)
        {
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
