using System.IO.Abstractions;
using AmiaReforged.Core.Models.Settlement;
using AmiaReforged.Settlements.Services.ResourceManagement;
using Anvil.Services;
using NLog;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AmiaReforged.Settlements.Services.Economy.FileReaders;

[ServiceBinding(typeof(IResourceImporter<EconomyItem>))]
[Obsolete("We decided not to define items in yaml files, so this class is no longer used.")]
public class EconomyItemImporter : IResourceImporter<EconomyItem>
{
    private readonly IFileSystem _fileSystem;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly Uri _files = StockpileConstants.EconomyItemFiles;
    private readonly IDeserializer _deserializer;

    public EconomyItemImporter(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem ?? new FileSystem();
        Log.Info("EconomyItemReader initialized.");
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }

    public IEnumerable<EconomyItem> LoadResources()
    {
        Log.Info("Loading Economy Items...");

        List<EconomyItem> importedItems = new();

        ReadYamlFiles(importedItems);

        return importedItems;
    }

    private void ReadYamlFiles(List<EconomyItem> loadResources)
    {
        string[] files = _fileSystem.Directory.GetFiles(_files.AbsolutePath, "*.yaml");
        Log.Info($"Found {files.Length} EconomyItem files.");
        Log.Info($"Path: {_files.AbsolutePath}");
        foreach (string file in files)
        {
            TryDeserializeItem(loadResources, file);
        }
    }

    private void TryDeserializeItem(List<EconomyItem> loadResources, string file)
    {
        using StreamReader reader = _fileSystem.File.OpenText(file);
        try
        {
            EconomyItem economyItem = _deserializer.Deserialize<EconomyItem>(reader);
            loadResources.Add(economyItem);
        }
        catch (Exception e)
        {
            Log.Error(e, $"Failed to load EconomyItem from file: {file}.");
        }
    }
}