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
            var existing = await context.PersistentObjects.FindAsync(obj.Id);
            if (existing == null)
            {
                context.PersistentObjects.Add(obj);
            }
            else
            {
                context.Entry(existing).CurrentValues.SetValues(obj);
                existing.Location = obj.Location;
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

            context.PersistentObjects.Remove(entity);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }

        return;
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

    List<PersistentObject> GetObjectsForArea(string areaResRef);
    bool AreaHasPersistentObjects(string areaResRef);
    Task<bool> SaveChanges();
}
