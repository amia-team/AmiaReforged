namespace AmiaReforged.PwEngine.Systems.WorldEngine;

public interface IDefinitionLoader
{
    void Load();
    List<FileLoadResult> Failures();
}
