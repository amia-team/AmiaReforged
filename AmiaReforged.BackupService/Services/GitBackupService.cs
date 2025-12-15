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

    public Task<bool> CommitAndPushAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            string repoPath = _config.GitRepositoryPath;
            
            if (!Repository.IsValid(repoPath))
            {
                _logger.LogError("Invalid git repository at {Path}", repoPath);
                return Task.FromResult(false);
            }

            using Repository repo = new(repoPath);
            
            // Stage all changes
            Commands.Stage(repo, "*");
            
            // Check if there are any changes to commit
            RepositoryStatus status = repo.RetrieveStatus();
            if (!status.IsDirty)
            {
                _logger.LogInformation("No changes to commit");
                return Task.FromResult(true);
            }
            
            int stagedCount = status.Staged.Count();
            _logger.LogInformation("Staging {Count} files for commit", stagedCount);

            // Create commit
            Signature author = new("Amia Backup Service", "backup@amia.local", DateTimeOffset.Now);
            string commitMessage = $"Automated backup - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
            
            Commit commit = repo.Commit(commitMessage, author, author);
            _logger.LogInformation("Created commit {Sha}: {Message}", commit.Sha[..8], commitMessage);

            // Push to remote
            Remote remote = repo.Network.Remotes[_config.GitRemote];
            if (remote == null)
            {
                _logger.LogError("Remote '{Remote}' not found", _config.GitRemote);
                return Task.FromResult(false);
            }

            // Get credentials from environment
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
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to commit and push backup");
            return Task.FromResult(false);
        }
    }
}
