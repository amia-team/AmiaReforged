namespace AmiaReforged.PwEngine.Systems.WorldEngine;

/// <summary>
/// Single-threaded repository since Resource Nodes are only loaded on startup or when a reload is forced.
/// </summary>
public interface IResourceNodeDefinitionRepository
{
    void Create(ResourceNodeDefinition definition);
    ResourceNodeDefinition Get(string tag);
    ResourceNodeDefinition Update(ResourceNodeDefinition definition);
    bool Delete(string tag);
    bool Exists(string tag);
}
