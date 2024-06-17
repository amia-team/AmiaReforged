using AmiaReforged.Core.Helpers;
using AmiaReforged.Core.Models.Settlement;
using AmiaReforged.Core.Services;
using AmiaReforged.Settlements.Services.Economy.FileReaders;
using Anvil.Services;

namespace AmiaReforged.Settlements.Services.Economy.Initialization;

[ServiceBinding(typeof(QualityInitializer))]
public class QualityInitializer : IResourceInitializer
{
    private readonly IResourceImporter<Quality> _importer;
    private readonly NwTaskHelper _taskHelper;
    private readonly Repository<Quality, int> _qualities;

    public QualityInitializer(IResourceImporter<Quality> importer, IRepositoryFactory repositoryFactory,
        NwTaskHelper taskHelper)
    {
        _importer = importer;
        _taskHelper = taskHelper;
        _qualities = ((Repository<Quality, int>?)repositoryFactory.CreateRepository<Quality, int>())!;
    }

    public async Task Initialize()
    {
        await ProcessQualities();
    }

    private async Task ProcessQualities()
    {
        foreach (Quality quality in _importer.LoadResources())
        {
            Quality? dbQual = await FindQuality(quality.Name);
            if (dbQual != null)
            {
                UpdateQuality(dbQual, quality);
                await _qualities.Update(dbQual);
            }
            else
            {
                await _qualities.Add(quality);
            }
        }
    }

    private async Task<Quality?> FindQuality(string qualityName)
    {
        IEnumerable<Quality?> qualities = await _qualities.GetAll();

        return qualities.FirstOrDefault(q => q?.Name == qualityName);
    }

    private static void UpdateQuality(Quality dbQual, Quality quality)
    {
        dbQual.ValueModifier = quality.ValueModifier;
        dbQual.Name = quality.Name;
    }
}