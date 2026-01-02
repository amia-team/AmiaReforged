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
    private readonly BackupConfig _config;

    public GitBackupService(ILogger<GitBackupService> logger, BackupConfig config)
    {
        _logger = logger;
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
