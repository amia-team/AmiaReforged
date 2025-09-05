namespace AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;

public interface INodeDefinitionLoader
{
    void Load();
    List<FileLoadResult> Failures();
}