using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
using AmiaReforged.PwEngine.Systems.JobSystem.Storage.Mapping;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Definitions.Common;

public class CharacterDataSource(NwPlayer player) : ILedgerDataSource
{
    [Inject] private JobSystemMappingService Mapper { get; set; } = null!;

    public List<LedgerEntry> LoadEntries()
    {
        NwCreature? creature = player.LoginCreature;
        if (creature == null) return new List<LedgerEntry>();
        JobSystemMappingService? mapper = AnvilCore.GetService<JobSystemMappingService>();
        if (mapper == null) return new List<LedgerEntry>();
        
        List<LedgerEntry> entries = new();
        
        // TODO: Figure out how to parse job system containers to ledger entries
        // List<NwItem> jobSystemContainers = creature.Inventory.Items.Where(i => i.ResRef == "TODO: REPLACE WITH JOB SYSTEM CONTAINER RESREF").ToList();
        
        List<NwItem> jobSystemItems = creature.Inventory.Items.Where(i => i.ResRef.Contains("js_")).ToList();
        
        List<JobItem> jobItems = jobSystemItems.Select(i => mapper.MapFrom(i)).ToList();

        Dictionary<ItemType, List<JobItem>> groupedItems = jobItems.GroupBy(i => i.Type).ToDictionary(g => g.Key, g => g.ToList());
        
        foreach (KeyValuePair<ItemType, List<JobItem>> group in groupedItems)
        {
            QualityEnum averageQuality = (QualityEnum) group.Value.Average(i => (decimal)i.Quality);
            
            LedgerEntry entry = new()
            {
                Type = group.Key,
                Name = group.Key.ToString(),
                Quantity = group.Value.Count,
                AverageQuality = averageQuality,
                BaseValue = group.Value.First().BaseValue,
                Items = group.Value.Select(i => new LedgerItem
                {
                    Name = i.Name,
                    QualityEnum = i.Quality,
                    MaterialEnum = i.Material,
                    MagicModifier = i.MagicModifier,
                    DurabilityModifier = i.DurabilityModifier,
                    BaseValue = i.BaseValue
                }).ToList()
            };
            
            entries.Add(entry);
        }
        
        return entries;
    }
}