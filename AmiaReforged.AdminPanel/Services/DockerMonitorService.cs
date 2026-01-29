using Docker.DotNet;
using Docker.DotNet.Models;
using System.Runtime.CompilerServices;
using System.Text;
using AmiaReforged.AdminPanel.Configuration;

namespace AmiaReforged.AdminPanel.Services;

/// <summary>
/// Container information DTO for the UI.
/// </summary>
public record ContainerInfo(
    string Id,
    string Name,
    string Image,
    string State,
    string Status,
    DateTime Created,
    bool IsMonitored
);

/// <summary>
/// Service for interacting with the Docker Engine API.
/// </summary>
public interface IDockerMonitorService
{
    Task<IReadOnlyList<ContainerInfo>> GetContainersAsync(CancellationToken ct = default);
    Task<ContainerInfo?> GetContainerAsync(string containerId, CancellationToken ct = default);
    IAsyncEnumerable<string> StreamLogsAsync(string containerId, bool follow = true, int tail = 100, CancellationToken ct = default);
    Task RestartContainerAsync(string containerId, CancellationToken ct = default);
    Task StartContainerAsync(string containerId, CancellationToken ct = default);
    Task StopContainerAsync(string containerId, CancellationToken ct = default);
}

public class DockerMonitorService : IDockerMonitorService, IDisposable
{
    private readonly IDockerClient _docker;
    private readonly ILogger<DockerMonitorService> _logger;

    public DockerMonitorService(
        AdminPanelConfig config,
        ILogger<DockerMonitorService> logger)
    {
        _logger = logger;
        _docker = new DockerClientConfiguration(new Uri(config.DockerSocketPath))
            .CreateClient();
    }

    public async Task<IReadOnlyList<ContainerInfo>> GetContainersAsync(CancellationToken ct = default)
    {
        var containers = await _docker.Containers.ListContainersAsync(
            new ContainersListParameters { All = true }, ct);

        return containers.Select(c => new ContainerInfo(
            Id: c.ID[..12],
            Name: c.Names.FirstOrDefault()?.TrimStart('/') ?? c.ID[..12],
            Image: c.Image,
            State: c.State,
            Status: c.Status,
            Created: c.Created,
            IsMonitored: false
        )).ToList();
    }

    public async Task<ContainerInfo?> GetContainerAsync(string containerId, CancellationToken ct = default)
    {
        try
        {
            var response = await _docker.Containers.InspectContainerAsync(containerId, ct);
            var uptime = response.State.Running && response.State.StartedAt != default
                ? DateTime.UtcNow - DateTime.Parse(response.State.StartedAt)
                : TimeSpan.Zero;

            return new ContainerInfo(
                Id: response.ID[..12],
                Name: response.Name.TrimStart('/'),
                Image: response.Config.Image,
                State: response.State.Status,
                Status: response.State.Running ? $"Up {uptime:d\\.hh\\:mm\\:ss}" : "Exited",
                Created: response.Created,
                IsMonitored: false
            );
        }
        catch (DockerContainerNotFoundException)
        {
            return null;
        }
    }

    public async IAsyncEnumerable<string> StreamLogsAsync(
        string containerId,
        bool follow = true,
        int tail = 100,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var parameters = new ContainerLogsParameters
        {
            ShowStdout = true,
            ShowStderr = true,
            Follow = follow,
            Tail = tail.ToString(),
            Timestamps = true
        };

        using var stream = await _docker.Containers.GetContainerLogsAsync(containerId, false, parameters, ct);

        var buffer = new byte[81920];
        while (!ct.IsCancellationRequested)
        {
            var result = await stream.ReadOutputAsync(buffer, 0, buffer.Length, ct);
            if (result.EOF)
                break;

            var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmed = line.TrimEnd('\r');
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    yield return trimmed;
                }
            }
        }
    }

    public async Task RestartContainerAsync(string containerId, CancellationToken ct = default)
    {
        _logger.LogInformation("Restarting container {ContainerId}", containerId);
        await _docker.Containers.RestartContainerAsync(containerId, new ContainerRestartParameters(), ct);
        _logger.LogInformation("Container {ContainerId} restarted", containerId);
    }

    public async Task StartContainerAsync(string containerId, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting container {ContainerId}", containerId);
        await _docker.Containers.StartContainerAsync(containerId, new ContainerStartParameters(), ct);
        _logger.LogInformation("Container {ContainerId} started", containerId);
    }

    public async Task StopContainerAsync(string containerId, CancellationToken ct = default)
    {
        _logger.LogInformation("Stopping container {ContainerId}", containerId);
        await _docker.Containers.StopContainerAsync(containerId, new ContainerStopParameters(), ct);
        _logger.LogInformation("Container {ContainerId} stopped", containerId);
    }

    public void Dispose()
    {
        _docker.Dispose();
    }
}
