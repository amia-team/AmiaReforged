using AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;

/// <summary>
/// Single-threaded repository since Resource Nodes are only loaded on startup or when a reload is forced.
/// </summary>
public interface IResourceNodeDefinitionRepository
{
    void Create(ResourceNodeDefinition definition);
    ResourceNodeDefinition? Get(string tag);
    void Update(ResourceNodeDefinition definition);
    bool Delete(string tag);
    bool Exists(string tag);
    List<ResourceNodeDefinition> All();
}
