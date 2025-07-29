using YamlDotNet.Serialization;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Economy.YamlLoaders;

public interface ILoader<T>
{
    List<T> LoadedResources { get; }
    List<ResourceLoadError> Failures { get; }
    string DirectoryPath { get; }
    void LoadAll();
}

public record ResourceLoadError(string FilePath, string ErrorMessage, Exception? Exception = null);
