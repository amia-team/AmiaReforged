using AmiaReforged.PwEngine.Database.Entities;
using Anvil.Services;
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
                .Where(po => po.Location != null && po.Location.AreaResRef == areaResRef)
                .ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            return [];
        }
    }

    public Task DeleteObject(PersistentObject obj)
    {
        using PwEngineContext context = factory.CreateDbContext();

        try
        {
            context.PersistentObjects.Remove(obj);
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }

        return Task.CompletedTask;
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
    Task DeleteObject(PersistentObject obj);

    List<PersistentObject> GetObjectsForArea(string areaResRef);
    bool AreaHasPersistentObjects(string areaResRef);
    Task<bool> SaveChanges();
}
