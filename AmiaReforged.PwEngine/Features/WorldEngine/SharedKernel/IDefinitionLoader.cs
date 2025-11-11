namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

public interface IDefinitionLoader
{
    void Load();
    List<FileLoadResult> Failures();
}
