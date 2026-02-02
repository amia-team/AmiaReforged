using AmiaReforged.PwEngine.Database.Entities.PlayerHousing;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.PwEngine.Database;

[ServiceBinding(typeof(IPlcLayoutRepository))]
public class PlcLayoutRepository : IPlcLayoutRepository
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly PwContextFactory _factory;

    public PlcLayoutRepository(PwContextFactory factory)
    {
        _factory = factory;
    }

    public async Task<List<PlcLayoutConfiguration>> GetLayoutsForPropertyAsync(
        Guid propertyId,
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        await using PwEngineContext context = _factory.CreateDbContext();

        try
        {
            return await context.PlcLayoutConfigurations
                .Where(l => l.PropertyId == propertyId && l.CharacterId == characterId)
                .OrderBy(l => l.Name)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to get layouts for property {PropertyId} and character {CharacterId}",
                propertyId, characterId);
            return [];
        }
    }

    public async Task<PlcLayoutConfiguration?> GetLayoutByIdAsync(
        long layoutId,
        CancellationToken cancellationToken = default)
    {
        await using PwEngineContext context = _factory.CreateDbContext();

        try
        {
            return await context.PlcLayoutConfigurations
                .Include(l => l.Items)
                .FirstOrDefaultAsync(l => l.Id == layoutId, cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to get layout with ID {LayoutId}", layoutId);
            return null;
        }
    }

    public async Task<PlcLayoutConfiguration> SaveLayoutAsync(
        PlcLayoutConfiguration layout,
        CancellationToken cancellationToken = default)
    {
        await using PwEngineContext context = _factory.CreateDbContext();

        try
        {
            layout.UpdatedUtc = DateTime.UtcNow;

            if (layout.Id == 0)
            {
                // New layout
                layout.CreatedUtc = DateTime.UtcNow;
                context.PlcLayoutConfigurations.Add(layout);
            }
            else
            {
                // Existing layout - remove old items and replace
                PlcLayoutConfiguration? existing = await context.PlcLayoutConfigurations
                    .Include(l => l.Items)
                    .FirstOrDefaultAsync(l => l.Id == layout.Id, cancellationToken);

                if (existing is not null)
                {
                    // Remove old items
                    context.PlcLayoutItems.RemoveRange(existing.Items);

                    // Update properties
                    existing.Name = layout.Name;
                    existing.UpdatedUtc = layout.UpdatedUtc;

                    // Add new items
                    foreach (PlcLayoutItem item in layout.Items)
                    {
                        item.LayoutConfigurationId = existing.Id;
                        context.PlcLayoutItems.Add(item);
                    }

                    layout = existing;
                }
                else
                {
                    // Layout was deleted, create new
                    layout.Id = 0;
                    layout.CreatedUtc = DateTime.UtcNow;
                    context.PlcLayoutConfigurations.Add(layout);
                }
            }

            await context.SaveChangesAsync(cancellationToken);
            return layout;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save layout configuration");
            throw;
        }
    }

    public async Task DeleteLayoutAsync(
        long layoutId,
        CancellationToken cancellationToken = default)
    {
        await using PwEngineContext context = _factory.CreateDbContext();

        try
        {
            PlcLayoutConfiguration? layout = await context.PlcLayoutConfigurations
                .Include(l => l.Items)
                .FirstOrDefaultAsync(l => l.Id == layoutId, cancellationToken);

            if (layout is not null)
            {
                context.PlcLayoutItems.RemoveRange(layout.Items);
                context.PlcLayoutConfigurations.Remove(layout);
                await context.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to delete layout with ID {LayoutId}", layoutId);
            throw;
        }
    }

    public async Task<int> CountLayoutsForPropertyAsync(
        Guid propertyId,
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        await using PwEngineContext context = _factory.CreateDbContext();

        try
        {
            return await context.PlcLayoutConfigurations
                .CountAsync(l => l.PropertyId == propertyId && l.CharacterId == characterId, cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to count layouts for property {PropertyId} and character {CharacterId}",
                propertyId, characterId);
            return 0;
        }
    }

    public async Task<bool> LayoutNameExistsAsync(
        Guid propertyId,
        Guid characterId,
        string name,
        long? excludeLayoutId = null,
        CancellationToken cancellationToken = default)
    {
        await using PwEngineContext context = _factory.CreateDbContext();

        try
        {
            IQueryable<PlcLayoutConfiguration> query = context.PlcLayoutConfigurations
                .Where(l => l.PropertyId == propertyId
                            && l.CharacterId == characterId
                            && l.Name == name);

            if (excludeLayoutId.HasValue)
            {
                query = query.Where(l => l.Id != excludeLayoutId.Value);
            }

            return await query.AnyAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to check layout name existence");
            return false;
        }
    }
}
