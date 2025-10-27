using AmiaReforged.PwEngine.Database.Entities;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Database;

[ServiceBinding(typeof(IStorageRepository))]
public class StorageRepository(PwContextFactory factory) : IStorageRepository
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public void AddItem(StoredItem item)
    {
        using PwEngineContext context = factory.CreateDbContext();

        try
        {
            context.WarehouseItems.Add(item);
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    public void BulkAddItems(List<StoredItem> items)
    {
        foreach (StoredItem item in items)
        {
            AddItem(item);
        }
    }

    public void RemoveItem(StoredItem item)
    {
        try
        {
            using PwEngineContext context = factory.CreateDbContext();
            context.WarehouseItems.Remove(item);
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    public StoredItem? GetItem(int id)
    {
        using PwEngineContext context = factory.CreateDbContext();
        return context.WarehouseItems.Find(id);
    }

    public List<StoredItem> GetItemsForOwner(Guid ownerId)
    {
        using PwEngineContext context = factory.CreateDbContext();
        return context.WarehouseItems.Where(i => i.Owner == ownerId).ToList();
    }

    public void ChangeOwner(StoredItem item, Guid newOwnerId)
    {
        try
        {
            using PwEngineContext context = factory.CreateDbContext();
            item.Owner = newOwnerId;
            context.WarehouseItems.Update(item);
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    public void BulkChangeOwner(List<StoredItem> items, Guid newOwnerId)
    {
        foreach (StoredItem item in items)
        {
            ChangeOwner(item, newOwnerId);
        }
    }

    public void SaveChanges()
    {
        try
        {
            using PwEngineContext context = factory.CreateDbContext();
            context.SaveChanges();
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }
}

public interface IStorageRepository
{
    void AddItem(StoredItem item);
    void BulkAddItems(List<StoredItem> items);
    void RemoveItem(StoredItem item);
    StoredItem? GetItem(int id);
    List<StoredItem> GetItemsForOwner(Guid ownerId);
    void ChangeOwner(StoredItem item, Guid newOwnerId);
    void BulkChangeOwner(List<StoredItem> items, Guid newOwnerId);
    void SaveChanges();
}
