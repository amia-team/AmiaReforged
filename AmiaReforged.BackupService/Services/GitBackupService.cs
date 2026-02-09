using System.Diagnostics;
using AmiaReforged.BackupService.Configuration;
using LibGit2Sharp;

namespace AmiaReforged.BackupService.Services;

/// <summary>
/// Service responsible for committing and pushing backups to git.
/// </summary>
public interface IGitBackupService
{
    /// <summary>
    /// Commits all changes in the backup directory and pushes to remote.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if commit and push succeeded</returns>
    Task<bool> CommitAndPushAsync(CancellationToken cancellationToken = default);
}

public class GitBackupService : IGitBackupService
{
    private readonly ILogger<GitBackupService> _logger;
    private readonly IDiscordNotificationService _discordService;
    private readonly BackupConfig _config;

    public GitBackupService(
        ILogger<GitBackupService> logger,
        IDiscordNotificationService discordService,
        BackupConfig config)
    {
        _logger = logger;
        _discordService = discordService;
        _config = config;
    }

    public async Task<bool> CommitAndPushAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            string repoPath = _config.GitRepositoryPath;

            if (!Repository.IsValid(repoPath))
            {
                _logger.LogError("Invalid git repository at {Path}", repoPath);
                return false;
            }

            // Check for and clean up stale lock file before proceeding
            if (!await HandleStaleLockFileAsync(repoPath, cancellationToken))
            {
                return false;
            }

            // Use native git commands for staging to handle Unicode filenames properly
            // LibGit2Sharp has the same .NET encoding issues with non-ASCII filenames
            if (!await RunGitCommandAsync(repoPath, "add -A", cancellationToken))
            {
                _logger.LogError("Failed to stage files with git add");
                return false;
            }

            // Check if there are any changes to commit using native git
            var (success, output) = await RunGitCommandWithOutputAsync(repoPath, "status --porcelain", cancellationToken);
            if (!success)
            {
                _logger.LogError("Failed to check git status");
                return false;
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                _logger.LogInformation("No changes to commit");
                return true;
            }

            int changedFiles = output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
            _logger.LogInformation("Staging {Count} changed files for commit", changedFiles);

            // Create commit using native git
            string commitMessage = $"Automated backup - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
            if (!await RunGitCommandAsync(repoPath, $"commit -m \"{commitMessage}\"", cancellationToken))
            {
                _logger.LogError("Failed to create commit");
                return false;
            }

            _logger.LogInformation("Created commit: {Message}", commitMessage);

            // Push using LibGit2Sharp for credential handling (it doesn't touch filenames)
            using Repository repo = new(repoPath);
            Remote? remote = repo.Network.Remotes[_config.GitRemote];
            if (remote == null)
            {
                _logger.LogError("Remote '{Remote}' not found", _config.GitRemote);
                return false;
            }

            string? gitUser = Environment.GetEnvironmentVariable("GIT_USER");
            string? gitToken = Environment.GetEnvironmentVariable("GIT_TOKEN");

            PushOptions pushOptions = new();

            if (!string.IsNullOrEmpty(gitUser) && !string.IsNullOrEmpty(gitToken))
            {
                pushOptions.CredentialsProvider = (_, _, _) =>
                    new UsernamePasswordCredentials
                    {
                        Username = gitUser,
                        Password = gitToken
                    };
            }

            string refSpec = $"refs/heads/{_config.GitBranch}";
            repo.Network.Push(remote, refSpec, pushOptions);

            _logger.LogInformation("Successfully pushed to {Remote}/{Branch}", _config.GitRemote, _config.GitBranch);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to commit and push backup");
            return false;
        }
    }

    /// <summary>
    /// Checks for a stale .git/index.lock file and removes it if it exists and is older than the threshold.
    /// This handles situations where a previous git process crashed or the service was killed mid-operation.
    /// </summary>
    private async Task<bool> HandleStaleLockFileAsync(string repoPath, CancellationToken cancellationToken)
    {
        string lockFilePath = Path.Combine(repoPath, ".git", "index.lock");

        if (!File.Exists(lockFilePath))
        {
            return true; // No lock file, all good
        }

        try
        {
            FileInfo lockFileInfo = new(lockFilePath);
            TimeSpan lockAge = DateTime.UtcNow - lockFileInfo.LastWriteTimeUtc;
            int thresholdMinutes = _config.GitLockStaleThresholdMinutes;

            _logger.LogWarning("Found git index.lock file at {Path}, age: {Age}",
                lockFilePath, lockAge);

            if (lockAge.TotalMinutes >= thresholdMinutes)
            {
                _logger.LogWarning("Lock file is stale (older than {Threshold} minutes), removing it",
                    thresholdMinutes);

                File.Delete(lockFilePath);

                string message = $"A stale git index.lock file was found and removed.\n\n" +
                                 $"**Path:** `{lockFilePath}`\n" +
                                 $"**Age:** {lockAge.TotalMinutes:F1} minutes\n" +
                                 $"**Threshold:** {thresholdMinutes} minutes\n\n" +
                                 "This typically happens when the server crashes during a backup operation. " +
                                 "The lock has been cleaned up and backup operations will continue.";

                await _discordService.SendWarningAsync(
                    "🔓 Stale Git Lock Removed",
                    message,
                    cancellationToken);

                _logger.LogInformation("Stale lock file removed successfully");
                return true;
            }
            else
            {
                // Lock file exists but isn't old enough to be considered stale
                // This could mean another git process is legitimately running
                _logger.LogError(
                    "Git index.lock exists but is only {Age} minutes old (threshold: {Threshold} minutes). " +
                    "Another git process may be running. Aborting to avoid conflicts.",
                    lockAge.TotalMinutes, thresholdMinutes);

                await _discordService.SendErrorAsync(
                    "⚠️ Git Lock Conflict",
                    $"A git index.lock file exists but is not old enough to be considered stale.\n\n" +
                    $"**Path:** `{lockFilePath}`\n" +
                    $"**Age:** {lockAge.TotalMinutes:F1} minutes\n" +
                    $"**Threshold:** {thresholdMinutes} minutes\n\n" +
                    "This backup cycle will be skipped. If no other git process should be running, " +
                    "the lock will be automatically removed once it exceeds the stale threshold.",
                    cancellationToken);

                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling git lock file at {Path}", lockFilePath);

            await _discordService.SendErrorAsync(
                "❌ Git Lock Error",
                $"Failed to handle git index.lock file.\n\n" +
                $"**Path:** `{lockFilePath}`\n" +
                $"**Error:** {ex.Message}\n\n" +
                "Manual intervention may be required.",
                cancellationToken);

            return false;
        }
    }

    private async Task<bool> RunGitCommandAsync(string workingDir, string arguments, CancellationToken cancellationToken)
    {
        var (success, _) = await RunGitCommandWithOutputAsync(workingDir, arguments, cancellationToken);
        return success;
    }

    private async Task<(bool Success, string Output)> RunGitCommandWithOutputAsync(string workingDir, string arguments, CancellationToken cancellationToken)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = workingDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            string error = await process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                _logger.LogDebug("Git command 'git {Args}' failed with exit code {Code}: {Error}",
                    arguments, process.ExitCode, error);
                return (false, output);
            }

            return (true, output);
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Git command 'git {Args}' threw exception: {Error}", arguments, ex.Message);
            return (false, string.Empty);
        }
    }
}
