using AmiaReforged.AdminPanel.Configuration;
using Microsoft.AspNetCore.Components.Forms;

namespace AmiaReforged.AdminPanel.Services;

/// <summary>
/// Result of a single file upload attempt.
/// </summary>
public sealed record FileUploadResult(string FileName, bool Success, string? Error = null);

/// <summary>
/// Result of an area upload batch operation.
/// </summary>
public sealed record AreaUploadResult(
    string TargetName,
    string TargetPath,
    IReadOnlyList<FileUploadResult> FileResults)
{
    public int SuccessCount => FileResults.Count(r => r.Success);
    public int FailureCount => FileResults.Count(r => !r.Success);
    public bool AllSucceeded => FileResults.All(r => r.Success);
}

/// <summary>
/// A single entry in a directory listing.
/// </summary>
public sealed record DirectoryEntry(string Name, long Size, DateTime LastModified, bool IsDirectory);

/// <summary>
/// Handles uploading area files (.are, .git, .gic) to pre-configured server directories.
/// Upload targets are defined in configuration via AREA_UPLOAD_PATH_* environment variables.
/// Only configured paths are writable — no user-supplied paths reach the filesystem.
/// </summary>
public sealed class AreaUploadService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".are", ".git", ".gic"
    };

    /// <summary>
    /// Maximum allowed file size per file (10 MB — area files are typically well under 1 MB).
    /// </summary>
    private const long MaxFileSize = 10 * 1024 * 1024;

    private readonly AdminPanelConfig _config;
    private readonly ILogger<AreaUploadService> _logger;

    public AreaUploadService(AdminPanelConfig config, ILogger<AreaUploadService> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Returns the list of configured upload targets.
    /// </summary>
    public IReadOnlyList<AreaUploadTarget> GetTargets() => _config.AreaUploadTargets;

    /// <summary>
    /// Lists the contents of the directory for the given target name.
    /// Returns an empty list if the target doesn't exist or the directory is missing.
    /// </summary>
    public IReadOnlyList<DirectoryEntry> ListTargetContents(string targetName)
    {
        AreaUploadTarget? target = _config.AreaUploadTargets.FirstOrDefault(
            t => string.Equals(t.Name, targetName, StringComparison.Ordinal));

        if (target is null)
        {
            _logger.LogWarning("Directory listing requested for unknown target: {TargetName}", targetName);
            return Array.Empty<DirectoryEntry>();
        }

        if (!Directory.Exists(target.Path))
        {
            _logger.LogInformation("Target directory does not exist yet: {Path}", target.Path);
            return Array.Empty<DirectoryEntry>();
        }

        try
        {
            List<DirectoryEntry> entries = new();

            foreach (string dir in Directory.GetDirectories(target.Path))
            {
                DirectoryInfo di = new(dir);
                entries.Add(new DirectoryEntry(di.Name, 0, di.LastWriteTime, true));
            }

            foreach (string file in Directory.GetFiles(target.Path))
            {
                FileInfo fi = new(file);
                entries.Add(new DirectoryEntry(fi.Name, fi.Length, fi.LastWriteTime, false));
            }

            return entries.OrderBy(e => !e.IsDirectory).ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list directory contents for target '{TargetName}' at {Path}", targetName, target.Path);
            return Array.Empty<DirectoryEntry>();
        }
    }

    /// <summary>
    /// Uploads the given files to the directory associated with the named target.
    /// Validates that the target exists in configuration and that each file has an allowed extension.
    /// </summary>
    public async Task<AreaUploadResult> UploadFilesAsync(string targetName, IReadOnlyList<IBrowserFile> files)
    {
        AreaUploadTarget? target = _config.AreaUploadTargets.FirstOrDefault(
            t => string.Equals(t.Name, targetName, StringComparison.Ordinal));

        if (target is null)
        {
            _logger.LogWarning("Upload attempted to unknown target: {TargetName}", targetName);
            List<FileUploadResult> rejected = files
                .Select(f => new FileUploadResult(f.Name, false, $"Unknown upload target: {targetName}"))
                .ToList();
            return new AreaUploadResult(targetName, string.Empty, rejected);
        }

        _logger.LogInformation("Uploading {FileCount} file(s) to target '{TargetName}' ({TargetPath})",
            files.Count, target.Name, target.Path);

        // Ensure destination directory exists
        Directory.CreateDirectory(target.Path);

        List<FileUploadResult> results = new(files.Count);

        foreach (IBrowserFile file in files)
        {
            results.Add(await UploadSingleFileAsync(file, target.Path));
        }

        int succeeded = results.Count(r => r.Success);
        int failed = results.Count(r => !r.Success);
        _logger.LogInformation("Upload complete for target '{TargetName}': {Succeeded} succeeded, {Failed} failed",
            target.Name, succeeded, failed);

        return new AreaUploadResult(target.Name, target.Path, results);
    }

    private async Task<FileUploadResult> UploadSingleFileAsync(IBrowserFile file, string targetDirectory)
    {
        string extension = Path.GetExtension(file.Name);

        if (!AllowedExtensions.Contains(extension))
        {
            _logger.LogWarning("Rejected file with disallowed extension: {FileName}", file.Name);
            return new FileUploadResult(file.Name, false,
                $"File type '{extension}' is not allowed. Only .are, .git, and .gic files are accepted.");
        }

        // Sanitize the filename — only allow the simple filename, strip any path components
        string safeFileName = Path.GetFileName(file.Name);
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            return new FileUploadResult(file.Name, false, "Invalid file name.");
        }

        string destinationPath = Path.Combine(targetDirectory, safeFileName);

        try
        {
            await using Stream sourceStream = file.OpenReadStream(MaxFileSize);
            await using FileStream destinationStream = new(destinationPath, FileMode.Create, FileAccess.Write);
            await sourceStream.CopyToAsync(destinationStream);

            _logger.LogInformation("Successfully wrote {FileName} to {DestinationPath}", safeFileName, destinationPath);
            return new FileUploadResult(safeFileName, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write {FileName} to {DestinationPath}", safeFileName, destinationPath);
            return new FileUploadResult(safeFileName, false, $"Write failed: {ex.Message}");
        }
    }
}
