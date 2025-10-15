using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes.ResourceNodeData;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes;

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
                return;
            }

            // Not tracked: check if it already exists in the database.
            bool existsInDb = _ctx.Set<PersistentResourceNodeInstance>()
                .AsNoTracking()
                .Any(e => e.Id == persistentInstance.Id);

            if (existsInDb)
            {
                // Attach a stub and set values so EF will issue an UPDATE instead of INSERT.
                PersistentResourceNodeInstance stub = new PersistentResourceNodeInstance { Id = persistentInstance.Id };
                _ctx.Attach(stub);
                _ctx.Entry(stub).CurrentValues.SetValues(persistentInstance);
            }
            else
            {
                // 3) New row: add so EF will issue an INSERT.
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
