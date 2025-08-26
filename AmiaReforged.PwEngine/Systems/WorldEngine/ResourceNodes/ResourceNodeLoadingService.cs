using AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;

// [ServiceBinding(typeof(ResourceNodeDefinitionLoadingService))]
public class ResourceNodeDefinitionLoadingService(IResourceNodeDefinitionRepository repository) : INodeDefinitionLoader
{
    private readonly List<FileLoadResult> _failures = [];
    public void Load()
    {

    }

    public List<FileLoadResult> Failures()
    {
        return _failures;
    }
}

public interface INodeDefinitionLoader
{
    void Load();
    List<FileLoadResult> Failures();
}


public record FileLoadResult(ResultType Type, string? Message = null, string? FileName = null);

public enum ResultType
{
    Success,
    Fail
}
