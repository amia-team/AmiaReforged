using System.Collections.Concurrent;
using System.Text.Json;
using AmiaReforged.AdminPanel.Configuration;
using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Services;

public interface IMonitoringConfigService
{
    Task<IReadOnlyList<MonitoredContainerConfig>> GetMonitoredContainersAsync(CancellationToken ct = default);
    Task<MonitoredContainerConfig?> GetMonitoredContainerAsync(string containerId, CancellationToken ct = default);
    Task<MonitoredContainerConfig> EnableMonitoringAsync(string containerId, string displayName, CancellationToken ct = default);
    Task DisableMonitoringAsync(string containerId, CancellationToken ct = default);
    Task UpdateMonitoringSettingsAsync(string containerId, bool autoRestart, string watchPatterns, CancellationToken ct = default);
    Task<bool> IsMonitoredAsync(string containerId, CancellationToken ct = default);
}

public class MonitoringConfigService : IMonitoringConfigService
{
    private readonly string _configPath;
    private readonly ILogger<MonitoringConfigService> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly ConcurrentDictionary<string, MonitoredContainerConfig> _containers = new();
    private bool _loaded;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public MonitoringConfigService(ILogger<MonitoringConfigService> logger, AdminPanelConfig config)
    {
        _logger = logger;
        _configPath = config.ConfigPath;
    }

    private async Task EnsureLoadedAsync(CancellationToken ct)
    {
        if (_loaded) return;

        await _lock.WaitAsync(ct);
        try
        {
            if (_loaded) return;

            if (File.Exists(_configPath))
            {
                var json = await File.ReadAllTextAsync(_configPath, ct);
                var state = JsonSerializer.Deserialize<MonitoringState>(json, JsonOptions);
                if (state?.Containers != null)
                {
                    foreach (var container in state.Containers)
                    {
                        _containers[container.ContainerId] = container;
                    }
                }
                _logger.LogInformation("Loaded {Count} monitored containers from {Path}", _containers.Count, _configPath);
            }
            else
            {
                _logger.LogInformation("No config file found at {Path}, starting fresh", _configPath);
            }
            _loaded = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load monitoring config from {Path}", _configPath);
            _loaded = true;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task SaveAsync(CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var state = new MonitoringState
            {
                Containers = _containers.Values.ToList()
            };

            var dir = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(state, JsonOptions);
            await File.WriteAllTextAsync(_configPath, json, ct);
            _logger.LogDebug("Saved monitoring config to {Path}", _configPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save monitoring config to {Path}", _configPath);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<MonitoredContainerConfig>> GetMonitoredContainersAsync(CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return _containers.Values.Where(c => c.IsMonitoringEnabled).OrderBy(c => c.DisplayName).ToList();
    }

    public async Task<MonitoredContainerConfig?> GetMonitoredContainerAsync(string containerId, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return _containers.TryGetValue(containerId, out var container) ? container : null;
    }

    public async Task<MonitoredContainerConfig> EnableMonitoringAsync(string containerId, string displayName, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        var config = _containers.GetOrAdd(containerId, _ => new MonitoredContainerConfig { ContainerId = containerId, DisplayName = displayName });
        config.IsMonitoringEnabled = true;
        config.DisplayName = displayName;
        await SaveAsync(ct);
        _logger.LogInformation("Enabled monitoring for container {ContainerId}", containerId);
        return config;
    }

    public async Task DisableMonitoringAsync(string containerId, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        if (_containers.TryGetValue(containerId, out var config))
        {
            config.IsMonitoringEnabled = false;
            await SaveAsync(ct);
            _logger.LogInformation("Disabled monitoring for container {ContainerId}", containerId);
        }
    }

    public async Task UpdateMonitoringSettingsAsync(string containerId, bool autoRestart, string watchPatterns, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        if (_containers.TryGetValue(containerId, out var config))
        {
            config.AutoRestartEnabled = autoRestart;
            config.WatchPatterns = watchPatterns;
            await SaveAsync(ct);
        }
    }

    public async Task<bool> IsMonitoredAsync(string containerId, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return _containers.TryGetValue(containerId, out var config) && config.IsMonitoringEnabled;
    }
}
