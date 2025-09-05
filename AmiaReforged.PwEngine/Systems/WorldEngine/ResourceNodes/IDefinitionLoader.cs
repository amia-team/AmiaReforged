namespace AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;

public interface IDefinitionLoader
{
    void Load();
    List<FileLoadResult> Failures();
}
