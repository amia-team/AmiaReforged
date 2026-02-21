using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using AmiaReforged.AdminPanel.Configuration;
using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Services;

/// <summary>
/// Background service that monitors container logs for crash patterns and triggers restarts.
/// </summary>
public class ContainerHealthMonitor : BackgroundService
{
    private readonly IDockerMonitorService _docker;
    private readonly IMonitoringConfigService _monitoringConfig;
    private readonly AdminPanelConfig _config;
    private readonly ILogger<ContainerHealthMonitor> _logger;

    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeMonitors = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastRestartTimes = new();

    public ContainerHealthMonitor(
        IDockerMonitorService docker,
        IMonitoringConfigService monitoringConfig,
        AdminPanelConfig config,
        ILogger<ContainerHealthMonitor> logger)
    {
        _docker = docker;
        _monitoringConfig = monitoringConfig;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Container Health Monitor starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncMonitoredContainersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing monitored containers");
            }

            await Task.Delay(TimeSpan.FromSeconds(_config.ContainerPollIntervalSeconds), stoppingToken);
        }

        // Cancel all active monitors on shutdown
        foreach (var cts in _activeMonitors.Values)
        {
            cts.Cancel();
        }
    }

    private async Task SyncMonitoredContainersAsync(CancellationToken ct)
    {
        var monitored = await _monitoringConfig.GetMonitoredContainersAsync(ct);
        var monitoredIds = monitored.Select(m => m.ContainerId).ToHashSet();

        // Stop monitoring containers that are no longer in the list
        foreach (var containerId in _activeMonitors.Keys.ToList())
        {
            if (!monitoredIds.Contains(containerId))
            {
                if (_activeMonitors.TryRemove(containerId, out var cts))
                {
                    _logger.LogInformation("Stopping log monitor for container {ContainerId}", containerId);
                    cts.Cancel();
                }
            }
        }

        // Start monitoring new containers
        foreach (var container in monitored)
        {
            if (!_activeMonitors.ContainsKey(container.ContainerId))
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                if (_activeMonitors.TryAdd(container.ContainerId, cts))
                {
                    _logger.LogInformation("Starting log monitor for container {ContainerId} ({DisplayName})",
                        container.ContainerId, container.DisplayName);
                    _ = MonitorContainerLogsAsync(container, cts.Token);
                }
            }
        }
    }

    private async Task MonitorContainerLogsAsync(MonitoredContainerConfig container, CancellationToken ct)
    {
        try
        {
            var patterns = container.WatchPatterns
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(p => new Regex(p, RegexOptions.IgnoreCase | RegexOptions.Compiled))
                .ToList();

            await foreach (var line in _docker.StreamLogsAsync(container.ContainerId, follow: true, tail: 0, ct: ct))
            {
                foreach (var pattern in patterns)
                {
                    if (pattern.IsMatch(line))
                    {
                        _logger.LogWarning("Pattern '{Pattern}' matched in container {ContainerId}: {Line}",
                            pattern.ToString(), container.ContainerId, line);

                        LogEvent(container.ContainerId, "PatternMatch",
                            $"Pattern: {pattern}, Line: {line[..Math.Min(line.Length, 500)]}");

                        if (container.AutoRestartEnabled && ShouldAutoRestart(container.ContainerId))
                        {
                            await HandleAutoRestartAsync(container, line, ct);
                        }

                        break; // Only match once per line
                    }
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogDebug("Log monitoring cancelled for container {ContainerId}", container.ContainerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error monitoring logs for container {ContainerId}", container.ContainerId);
        }
        finally
        {
            _activeMonitors.TryRemove(container.ContainerId, out _);
        }
    }

    private bool ShouldAutoRestart(string containerId)
    {
        if (_lastRestartTimes.TryGetValue(containerId, out var lastRestart))
        {
            var elapsed = DateTime.UtcNow - lastRestart;
            if (elapsed.TotalSeconds < _config.AutoRestartCooldownSeconds)
            {
                _logger.LogInformation("Skipping auto-restart for {ContainerId}: cooldown active ({Remaining}s remaining)",
                    containerId, _config.AutoRestartCooldownSeconds - elapsed.TotalSeconds);
                return false;
            }
        }
        return true;
    }

    private async Task HandleAutoRestartAsync(MonitoredContainerConfig container, string triggerLine, CancellationToken ct)
    {
        try
        {
            _logger.LogWarning("Auto-restarting container {ContainerId} due to crash pattern detection", container.ContainerId);

            _lastRestartTimes[container.ContainerId] = DateTime.UtcNow;

            await _docker.RestartContainerAsync(container.ContainerId, ct);

            LogEvent(container.ContainerId, "AutoRestart",
                $"Triggered by pattern match: {triggerLine[..Math.Min(triggerLine.Length, 500)]}");

            // TODO: Send Discord notification if configured
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-restart container {ContainerId}", container.ContainerId);
            LogEvent(container.ContainerId, "AutoRestartFailed", ex.Message);
        }
    }

    private void LogEvent(string containerId, string eventType, string? details)
    {
        _logger.LogInformation("Container event: {ContainerId} | {EventType} | {Details}",
            containerId, eventType, details);
    }
}
