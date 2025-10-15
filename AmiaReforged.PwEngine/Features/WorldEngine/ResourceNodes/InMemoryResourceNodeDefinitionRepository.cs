using AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes.ResourceNodeData;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes;

[ServiceBinding(typeof(IResourceNodeDefinitionRepository))]
public class InMemoryResourceNodeDefinitionRepository : IResourceNodeDefinitionRepository
{
    private readonly Dictionary<string, ResourceNodeDefinition> _definitions = new();

    public void Create(ResourceNodeDefinition definition)
    {
        bool added = _definitions.TryAdd(definition.Tag, definition);

        if (!added)
        {
            Update(definition);
        }
    }

    public ResourceNodeDefinition? Get(string tag)
    {
        return _definitions.GetValueOrDefault(tag);
    }

    public void Update(ResourceNodeDefinition definition)
    {
        ResourceNodeDefinition? existing = Get(definition.Tag);

        if (existing is null)
        {
            Create(definition);
            return;
        }

        _definitions[definition.Tag] = definition;
    }

    public bool Delete(string tag)
    {
        return _definitions.Remove(tag);
    }

    public bool Exists(string tag)
    {
        return Get(tag) != null;
    }

    public List<ResourceNodeDefinition> All()
    {
        return _definitions.Values.ToList();
    }
}
