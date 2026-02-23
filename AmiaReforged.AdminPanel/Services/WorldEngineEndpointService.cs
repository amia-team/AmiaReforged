using System.Collections.Concurrent;
using System.Text.Json;
using AmiaReforged.AdminPanel.Configuration;
using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Services;

public interface IWorldEngineEndpointService
{
    /// <summary>Get all registered endpoints (enabled and disabled).</summary>
    Task<IReadOnlyList<WorldEngineEndpoint>> GetAllEndpointsAsync(CancellationToken ct = default);

    /// <summary>Get only enabled endpoints.</summary>
    Task<IReadOnlyList<WorldEngineEndpoint>> GetEnabledEndpointsAsync(CancellationToken ct = default);

    /// <summary>Get a single endpoint by id.</summary>
    Task<WorldEngineEndpoint?> GetEndpointAsync(Guid id, CancellationToken ct = default);

    /// <summary>Add a new endpoint. Returns the created endpoint.</summary>
    Task<WorldEngineEndpoint> AddEndpointAsync(string name, string baseUrl, string? apiKey = null, CancellationToken ct = default);

    /// <summary>Update an existing endpoint's name, URL, API key, or enabled state.</summary>
    Task<WorldEngineEndpoint?> UpdateEndpointAsync(Guid id, string? name, string? baseUrl, bool? isEnabled, string? apiKey = null, bool clearApiKey = false, CancellationToken ct = default);

    /// <summary>Remove an endpoint entirely.</summary>
    Task<bool> RemoveEndpointAsync(Guid id, CancellationToken ct = default);
}

/// <summary>
/// Persists WorldEngine server endpoints to a JSON file on disk,
/// following the same lazy-load + in-memory cache pattern as
/// <see cref="MonitoringConfigService"/>.
/// </summary>
public class WorldEngineEndpointService : IWorldEngineEndpointService
{
    private readonly string _configPath;
    private readonly ILogger<WorldEngineEndpointService> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly ConcurrentDictionary<Guid, WorldEngineEndpoint> _endpoints = new();
    private bool _loaded;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public WorldEngineEndpointService(ILogger<WorldEngineEndpointService> logger, AdminPanelConfig config)
    {
        _logger = logger;

        // Store alongside the monitoring config in the same data directory
        var dir = Path.GetDirectoryName(config.ConfigPath) ?? "/data";
        _configPath = Path.Combine(dir, "worldengine-endpoints.json");
    }

    // ==================== Public API ====================

    public async Task<IReadOnlyList<WorldEngineEndpoint>> GetAllEndpointsAsync(CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return _endpoints.Values.OrderBy(e => e.Name).ToList();
    }

    public async Task<IReadOnlyList<WorldEngineEndpoint>> GetEnabledEndpointsAsync(CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return _endpoints.Values.Where(e => e.IsEnabled).OrderBy(e => e.Name).ToList();
    }

    public async Task<WorldEngineEndpoint?> GetEndpointAsync(Guid id, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return _endpoints.TryGetValue(id, out var ep) ? ep : null;
    }

    public async Task<WorldEngineEndpoint> AddEndpointAsync(string name, string baseUrl, string? apiKey = null, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);

        var endpoint = new WorldEngineEndpoint
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            BaseUrl = baseUrl.TrimEnd('/'),
            ApiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey.Trim(),
            IsEnabled = true
        };

        _endpoints[endpoint.Id] = endpoint;
        await SaveAsync(ct);

        _logger.LogInformation("Added WorldEngine endpoint '{Name}' → {Url}", endpoint.Name, endpoint.BaseUrl);
        return endpoint;
    }

    public async Task<WorldEngineEndpoint?> UpdateEndpointAsync(Guid id, string? name, string? baseUrl, bool? isEnabled, string? apiKey = null, bool clearApiKey = false, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);

        if (!_endpoints.TryGetValue(id, out var ep)) return null;

        if (name != null) ep.Name = name.Trim();
        if (baseUrl != null) ep.BaseUrl = baseUrl.TrimEnd('/');
        if (isEnabled.HasValue) ep.IsEnabled = isEnabled.Value;
        if (clearApiKey) ep.ApiKey = null;
        else if (apiKey != null) ep.ApiKey = apiKey.Trim();

        await SaveAsync(ct);
        _logger.LogInformation("Updated WorldEngine endpoint '{Name}' ({Id})", ep.Name, ep.Id);
        return ep;
    }

    public async Task<bool> RemoveEndpointAsync(Guid id, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);

        if (!_endpoints.TryRemove(id, out var removed)) return false;

        await SaveAsync(ct);
        _logger.LogInformation("Removed WorldEngine endpoint '{Name}' ({Id})", removed.Name, removed.Id);
        return true;
    }

    // ==================== Persistence ====================

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
                var state = JsonSerializer.Deserialize<WorldEngineEndpointState>(json, JsonOptions);
                if (state?.Endpoints != null)
                {
                    foreach (var ep in state.Endpoints)
                    {
                        _endpoints[ep.Id] = ep;
                    }
                }
                _logger.LogInformation("Loaded {Count} WorldEngine endpoints from {Path}", _endpoints.Count, _configPath);
            }
            else
            {
                _logger.LogInformation("No endpoint config found at {Path}, starting with empty list", _configPath);
            }

            _loaded = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load WorldEngine endpoints from {Path}", _configPath);
            _loaded = true; // Don't retry on every call
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
            var state = new WorldEngineEndpointState
            {
                Endpoints = _endpoints.Values.OrderBy(e => e.Name).ToList()
            };

            var dir = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(state, JsonOptions);
            await File.WriteAllTextAsync(_configPath, json, ct);
            _logger.LogDebug("Saved WorldEngine endpoints to {Path}", _configPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save WorldEngine endpoints to {Path}", _configPath);
        }
        finally
        {
            _lock.Release();
        }
    }
}
