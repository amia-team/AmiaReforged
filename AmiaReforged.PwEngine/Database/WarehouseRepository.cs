using AmiaReforged.PwEngine.Database.Entities;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Database;

[ServiceBinding(typeof(IWarehouseRepository))]
public class WarehouseRepository(PwContextFactory factory) : IWarehouseRepository
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public void AddWarehouse(Storage warehouse, bool includeItems = true)
    {
        try
        {
            using PwEngineContext context = factory.CreateDbContext();
            context.Warehouses.Add(warehouse);
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    public bool DeleteWarehouse(Storage warehouse, bool includeItems = false)
    {
        try
        {
            using PwEngineContext context = factory.CreateDbContext();
            context.Warehouses.Remove(warehouse);
            return true;
        }
        catch (Exception e)
        {
            Log.Error(e);
            return false;
        }
    }

    public Storage? GetWarehouse(long id)
    {
        try
        {
            using PwEngineContext context = factory.CreateDbContext();
            return context.Warehouses.Find(id);
        }
        catch (Exception e)
        {
            Log.Error(e);
            return null;
        }
    }

    public Storage? GetWarehouseForOwner(Guid ownerId)
    {
        try
        {
            using PwEngineContext context = factory.CreateDbContext();
            return context.Warehouses.FirstOrDefault(w => w.OwnerId == ownerId);
        }
        catch (Exception e)
        {
            Log.Error(e);
            return null;
        }
    }

    public bool SaveChanges()
    {
        try
        {
            using PwEngineContext context = factory.CreateDbContext();
            context.SaveChanges();
            return true;
        }
        catch (Exception e)
        {
            Log.Error(e);
            return false;
        }
    }
}

public interface IWarehouseRepository
{
    void AddWarehouse(Storage warehouse, bool includeItems = true);
    bool DeleteWarehouse(Storage warehouse, bool includeItems = false);
    Storage? GetWarehouse(long id);
    Storage? GetWarehouseForOwner(Guid ownerId);
    bool SaveChanges();
}
