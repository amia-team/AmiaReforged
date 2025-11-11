using AmiaReforged.PwEngine.Database.Entities;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.PwEngine.Database;

[ServiceBinding(typeof(IPersistentObjectRepository))]
public class PersistentObjectRepository(PwContextFactory factory) : IPersistentObjectRepository
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public async Task SaveObject(PersistentObject obj)
    {
        await using PwEngineContext context = factory.CreateDbContext();

        try
        {
            PersistentObject? existing = await context.PersistentObjects.FindAsync(obj.Id);
            if (existing == null)
            {
                context.PersistentObjects.Add(obj);
            }
            else
            {
                context.Entry(existing).CurrentValues.SetValues(obj);

                if (obj.Location is not null)
                {
                    if (existing.Location is null)
                    {
                        existing.Location = obj.Location;
                    }
                    else
                    {
                        context.Entry(existing.Location).CurrentValues.SetValues(obj.Location);
                    }
                }
            }

            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }
    }

    public List<PersistentObject> GetObjectsForArea(string areaResRef)
    {
        using PwEngineContext context = factory.CreateDbContext();

        try
        {
            return context.PersistentObjects
                .Include(po => po.Location)
                .Where(po => po.Location != null && po.Location.AreaResRef == areaResRef)
                .ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            return [];
        }
    }

    public async Task DeleteObject(long id)
    {
        await using PwEngineContext context = factory.CreateDbContext();

        try
        {
            PersistentObject? entity = await context.PersistentObjects
                .Include(p => p.Location)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (entity is null)
            {
                return;
            }

            if (entity.Location is not null)
            {
                context.Set<SavedLocation>().Remove(entity.Location);
            }

            context.PersistentObjects.Remove(entity);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }

        return;
    }

    public async Task<PersistentObject?> GetObject(long id)
    {
        await using PwEngineContext context = factory.CreateDbContext();

        try
        {
            return await context.PersistentObjects
                .Include(po => po.Location)
                .FirstOrDefaultAsync(po => po.Id == id);
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            return null;
        }
    }

    public bool AreaHasPersistentObjects(string areaResRef)
    {
        using PwEngineContext context = factory.CreateDbContext();

        try
        {
            return context.PersistentObjects
                .Any(po => po.Location != null && po.Location.AreaResRef == areaResRef);
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            return false;
        }
    }

    public Task<bool> SaveChanges()
    {
        using PwEngineContext context = factory.CreateDbContext();

        try
        {
            context.SaveChanges();
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            return Task.FromResult(false);
        }
    }
}

public interface IPersistentObjectRepository
{
    Task SaveObject(PersistentObject obj);
    Task DeleteObject(long id);
    Task<PersistentObject?> GetObject(long id);

    List<PersistentObject> GetObjectsForArea(string areaResRef);
    bool AreaHasPersistentObjects(string areaResRef);
    Task<bool> SaveChanges();
}
