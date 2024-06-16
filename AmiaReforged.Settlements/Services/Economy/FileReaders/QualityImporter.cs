using System.IO.Abstractions;
using AmiaReforged.Core.Models.Settlement;
using AmiaReforged.Settlements.Services.ResourceManagement;
using Anvil.Services;
using NLog;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AmiaReforged.Settlements.Services.Economy.FileReaders;

[ServiceBinding(typeof(IResourceImporter<Quality>))]
public class QualityImporter : IResourceImporter<Quality>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly IFileSystem _fileSystem;
    private readonly IDeserializer _deserializer;

    public QualityImporter(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem ?? new FileSystem();
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        Log.Info("QualityImporter initialized.");
    }

    public IEnumerable<Quality> LoadResources()
    {
        List<Quality> importedQualities = ReadYamlFiles();

        return importedQualities;
    }

    private List<Quality> ReadYamlFiles()
    {
        string[] files = _fileSystem.Directory.GetFiles(StockpileConstants.QualityFiles.AbsolutePath, "*.yaml");

        return files.Select(TryDeserializeQuality).OfType<Quality>().ToList();
    }

    private Quality? TryDeserializeQuality(string file)
    {
        using StreamReader reader = _fileSystem.File.OpenText(file);
        try
        {
            Quality quality = _deserializer.Deserialize<Quality>(reader);
            return quality;
        }
        catch (Exception e)
        {
            Log.Error(e, $"Failed to load Quality from file: {file}.");
            return null;
        }
    }
}