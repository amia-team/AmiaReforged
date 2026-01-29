using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using AmiaReforged.AdminPanel.Services;

namespace AmiaReforged.AdminPanel.Hubs;

[Authorize]
public class ContainerLogHub : Hub
{
    private readonly IDockerMonitorService _docker;
    private readonly ILogger<ContainerLogHub> _logger;

    public ContainerLogHub(IDockerMonitorService docker, ILogger<ContainerLogHub> logger)
    {
        _docker = docker;
        _logger = logger;
    }

    public async IAsyncEnumerable<string> StreamLogs(string containerId, int tail = 100)
    {
        var connectionId = Context.ConnectionId;
        _logger.LogInformation("Client {ConnectionId} started streaming logs for {ContainerId}", connectionId, containerId);

        var cts = CancellationTokenSource.CreateLinkedTokenSource(Context.ConnectionAborted);

        await foreach (var line in _docker.StreamLogsAsync(containerId, follow: true, tail: tail, ct: cts.Token))
        {
            yield return line;
        }

        _logger.LogInformation("Client {ConnectionId} stopped streaming logs for {ContainerId}", connectionId, containerId);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogDebug("Client {ConnectionId} disconnected", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
