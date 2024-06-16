using System.IO.Abstractions;
using AmiaReforged.Core.Models.Settlement;
using AmiaReforged.Settlements.Services.ResourceManagement;
using Anvil.Services;
using NLog;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AmiaReforged.Settlements.Services.Economy.FileReaders;

[ServiceBinding(typeof(IResourceImporter<Material>))]
public class MaterialImporter : IResourceImporter<Material>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly IFileSystem _fileSystem;
    private readonly IDeserializer _deserializer;
    private readonly Uri _files = StockpileConstants.MaterialFiles;

    public MaterialImporter(IFileSystem mockFileSystem)
    {
        _fileSystem = mockFileSystem ?? new FileSystem();
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        Log.Info("MaterialImporter initialized.");
    }

    public IEnumerable<Material> LoadResources()
    {
        List<Material> importedMaterials = ReadYamlFiles();

        return importedMaterials;
    }

    private List<Material> ReadYamlFiles()
    {
        string[] files = _fileSystem.Directory.GetFiles(_files.AbsolutePath, "*.yaml");

        return files.Select(TryDeserializeMaterial).OfType<Material>().ToList();
    }

    private Material? TryDeserializeMaterial(string file)
    {
        using StreamReader reader = _fileSystem.File.OpenText(file);
        try
        {
            Material material = _deserializer.Deserialize<Material>(reader);
            return material;
        }
        catch (Exception e)
        {
            Log.Error(e, $"Failed to load Material from file: {file}.");
            return null;
        }
    }
}