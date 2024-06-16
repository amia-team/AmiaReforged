namespace AmiaReforged.Settlements.Services.Economy.FileReaders;

public interface IResourceImporter<out T>
{
    IEnumerable<T> LoadResources();
}