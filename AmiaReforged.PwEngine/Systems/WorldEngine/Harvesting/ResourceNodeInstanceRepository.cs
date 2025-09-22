using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;
using Anvil.Services;
using NLog;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;

/// <summary>
/// Database repository for <see cref="IResourceNodeInstanceRepository"/>
/// </summary>
[ServiceBinding(typeof(IResourceNodeInstanceRepository))]
public class ResourceNodeInstanceRepository(PwContextFactory factory, ResourceNodeMappingHelper helper)
    : IResourceNodeInstanceRepository
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly PwEngineContext _ctx = factory.CreateDbContext();

    private PersistentResourceNodeInstance? FindTracked(Guid id) =>
        _ctx.ChangeTracker.Entries<PersistentResourceNodeInstance>()
            .FirstOrDefault(e => e.Entity.Id == id)?.Entity;

    public void AddNodeInstance(ResourceNodeInstance instance)
    {
        PersistentResourceNodeInstance persistentInstance = helper.MapFrom(instance);
        try
        {
            // If an entity with the same key is already tracked, update its values.
            PersistentResourceNodeInstance? tracked = FindTracked(persistentInstance.Id);
            if (tracked is not null)
            {
                _ctx.Entry(tracked).CurrentValues.SetValues(persistentInstance);
            }
            else
            {
                _ctx.Add(persistentInstance);
            }
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    public void RemoveNodeInstance(ResourceNodeInstance instance)
    {
        PersistentResourceNodeInstance persistentInstance = helper.MapFrom(instance);
        try
        {
            PersistentResourceNodeInstance? tracked = FindTracked(persistentInstance.Id);
            if (tracked is not null)
            {
                _ctx.Remove(tracked);
            }
            else
            {
                _ctx.Attach(persistentInstance);
                _ctx.Remove(persistentInstance);
            }
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    public List<ResourceNodeInstance> GetInstances()
    {
        // No-tracking read to avoid populating the ChangeTracker with entities
        List<PersistentResourceNodeInstance> db = _ctx.PersistedNodes
            .AsNoTracking()
            .ToList();

        List<ResourceNodeInstance> result = [];

        foreach (PersistentResourceNodeInstance p in db)
        {
            ResourceNodeInstance? instance = helper.MapTo(p);

            if (instance == null) continue;

            result.Add(instance);
        }

        return result;
    }

    public List<ResourceNodeInstance> GetInstancesByArea(string resRef)
    {
        return GetInstances().Where(i => i.Area == resRef).ToList();
    }

    public void Update(ResourceNodeInstance dataNodeInstance)
    {
        PersistentResourceNodeInstance persistentInstance = helper.MapFrom(dataNodeInstance);

        try
        {
            PersistentResourceNodeInstance? tracked = FindTracked(persistentInstance.Id);
            if (tracked is not null)
            {
                // Update the already-tracked entity
                _ctx.Entry(tracked).CurrentValues.SetValues(persistentInstance);
            }
            else
            {
                // Attach and mark as modified (no duplicate tracked instance)
                _ctx.Attach(persistentInstance);
                _ctx.Entry(persistentInstance).State = EntityState.Modified;
            }
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    public bool SaveChanges()
    {
        try
        {
            _ctx.SaveChanges();
            return true;
        }
        catch (Exception e)
        {
            Log.Error(e);
            return false;
        }
    }

    public void Delete(ResourceNodeInstance instance)
    {
        PersistentResourceNodeInstance persistentInstance = helper.MapFrom(instance);

        try
        {
            PersistentResourceNodeInstance? tracked = FindTracked(persistentInstance.Id);
            if (tracked is not null)
            {
                _ctx.Remove(tracked);
            }
            else
            {
                _ctx.Attach(persistentInstance);
                _ctx.Remove(persistentInstance);
            }
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }
}
