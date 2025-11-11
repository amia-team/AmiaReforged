using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes;

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
