using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;

/// <summary>
/// Database repository for <see cref="IResourceNodeInstanceRepository"/>
/// </summary>
[ServiceBinding(typeof(IResourceNodeInstanceRepository))]
public class ResourceNodeInstanceRepository(PwContextFactory factory, ResourceNodeMappingHelper helper)
    : IResourceNodeInstanceRepository
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private PwEngineContext _ctx = factory.CreateDbContext();

    public void AddNodeInstance(ResourceNodeInstance instance)
    {
        PersistentResourceNodeInstance persistentInstance = helper.MapFrom(instance);
        try
        {
            _ctx.Add(persistentInstance);
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
            _ctx.Remove(persistentInstance);
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    public List<ResourceNodeInstance> GetInstances()
    {
        List<PersistentResourceNodeInstance> db = _ctx.PersistedNodes.ToList();
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
            _ctx.Update(persistentInstance);
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
            _ctx.Remove(persistentInstance);
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }
}

[ServiceBinding(typeof(ResourceNodeMappingHelper))]
public class ResourceNodeMappingHelper(IResourceNodeDefinitionRepository definitionRepository)
{
    public PersistentResourceNodeInstance MapFrom(ResourceNodeInstance node)
    {
        return new PersistentResourceNodeInstance
        {
            Id = node.Id,
            Area = node.Area,
            DefinitionTag = node.Definition.Tag,
            Uses = node.Uses,
            Quality = (int)node.Quality,
            X = node.X,
            Y = node.Y,
            Z = node.Z,
            Rotation = node.Rotation,
        };
    }

    public ResourceNodeInstance? MapTo(PersistentResourceNodeInstance instance)
    {
        ResourceNodeDefinition? definition = definitionRepository.Get(instance.DefinitionTag);

        if (definition == null)
        {
            return null;
        }

        return new ResourceNodeInstance
        {
            Id = instance.Id,
            Area = instance.Area,
            Definition = definition,
            Uses = instance.Uses,
            X = instance.X,
            Y = instance.Y,
            Z = instance.Z,
            Quality = (IPQuality)instance.Quality,
            Rotation = instance.Rotation,
        };
    }
}
