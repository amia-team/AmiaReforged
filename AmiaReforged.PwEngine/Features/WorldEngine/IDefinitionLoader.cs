namespace AmiaReforged.PwEngine.Features.WorldEngine;

public interface IDefinitionLoader
{
    void Load();
    List<FileLoadResult> Failures();
}
