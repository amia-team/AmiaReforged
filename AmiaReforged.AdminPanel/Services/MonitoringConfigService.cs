using AmiaReforged.AdminPanel.Data;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.AdminPanel.Services;

public interface IMonitoringConfigService
{
    Task<IReadOnlyList<MonitoredContainer>> GetMonitoredContainersAsync(CancellationToken ct = default);
    Task<MonitoredContainer?> GetMonitoredContainerAsync(string containerId, CancellationToken ct = default);
    Task<MonitoredContainer> EnableMonitoringAsync(string containerId, string displayName, CancellationToken ct = default);
    Task DisableMonitoringAsync(string containerId, CancellationToken ct = default);
    Task UpdateMonitoringSettingsAsync(string containerId, bool autoRestart, string watchPatterns, CancellationToken ct = default);
    Task<bool> IsMonitoredAsync(string containerId, CancellationToken ct = default);
}

public class MonitoringConfigService : IMonitoringConfigService
{
    private readonly IDbContextFactory<AdminPanelDbContext> _contextFactory;
    private readonly ILogger<MonitoringConfigService> _logger;

    public MonitoringConfigService(IDbContextFactory<AdminPanelDbContext> contextFactory, ILogger<MonitoringConfigService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<MonitoredContainer>> GetMonitoredContainersAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.MonitoredContainers.Where(c => c.IsMonitoringEnabled).OrderBy(c => c.DisplayName).ToListAsync(ct);
    }

    public async Task<MonitoredContainer?> GetMonitoredContainerAsync(string containerId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.MonitoredContainers.FirstOrDefaultAsync(c => c.ContainerId == containerId, ct);
    }

    public async Task<MonitoredContainer> EnableMonitoringAsync(string containerId, string displayName, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var existing = await context.MonitoredContainers.FirstOrDefaultAsync(c => c.ContainerId == containerId, ct);
        if (existing != null)
        {
            existing.IsMonitoringEnabled = true;
            existing.DisplayName = displayName;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            existing = new MonitoredContainer { ContainerId = containerId, DisplayName = displayName, IsMonitoringEnabled = true };
            context.MonitoredContainers.Add(existing);
        }
        await context.SaveChangesAsync(ct);
        _logger.LogInformation("Enabled monitoring for container {ContainerId}", containerId);
        return existing;
    }

    public async Task DisableMonitoringAsync(string containerId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var container = await context.MonitoredContainers.FirstOrDefaultAsync(c => c.ContainerId == containerId, ct);
        if (container != null)
        {
            container.IsMonitoringEnabled = false;
            container.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(ct);
            _logger.LogInformation("Disabled monitoring for container {ContainerId}", containerId);
        }
    }

    public async Task UpdateMonitoringSettingsAsync(string containerId, bool autoRestart, string watchPatterns, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var container = await context.MonitoredContainers.FirstOrDefaultAsync(c => c.ContainerId == containerId, ct);
        if (container != null)
        {
            container.AutoRestartEnabled = autoRestart;
            container.WatchPatterns = watchPatterns;
            container.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(ct);
        }
    }

    public async Task<bool> IsMonitoredAsync(string containerId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.MonitoredContainers.AnyAsync(c => c.ContainerId == containerId && c.IsMonitoringEnabled, ct);
    }
}
