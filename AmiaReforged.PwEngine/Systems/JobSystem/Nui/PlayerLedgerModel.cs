using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions.Common;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.JobSystem.Nui;

[CreatedAtRuntime]
public sealed class PlayerLedgerModel(NwPlayer player)
{
    private CharacterLedger Ledger { get; set; } = new(new CharacterDataSource(player));
    
    public void RefreshLedger()
    {
        Ledger = new CharacterLedger(new CharacterDataSource(player));
    }

    public LedgerCategoryViewModel? ViewModelFor(ItemType type)
    {
        // We need a dictionary of materials and their corresponding item types

        LedgerEntry? ledgerEntry = Ledger.Entries.FirstOrDefault(e => e.Type == type);

        if (ledgerEntry == null) return null;

        return LedgerCategoryViewModel.Create(ledgerEntry);
    }
}

/// <summary>
/// Splits a ledger entry into a dictionary of materials and their corresponding items.
/// </summary>
public sealed class LedgerCategoryViewModel
{
    private LedgerCategoryViewModel(LedgerEntry entry)
    {
        // We need a dictionary of materials and their corresponding item types
        Dictionary<MaterialEnum, List<LedgerItem>> groupedItems =
            entry.Items.GroupBy(i => i.MaterialEnum).ToDictionary(g => g.Key, g => g.ToList());
        // Now we need to just populate our class with the grouped items
        foreach (KeyValuePair<MaterialEnum, List<LedgerItem>> group in groupedItems)
        {
            CategoryEntries.TryAdd(group.Key, new LedgerSubEntryViewModel(group.Value));
        }
    }

    public Dictionary<MaterialEnum, LedgerSubEntryViewModel> CategoryEntries { get; } = new();

    public static LedgerCategoryViewModel Create(LedgerEntry entry) => new(entry);
}

/// <summary>
/// Provides a view model for a ledger sub-entry.
/// </summary>
/// <param name="entries"></param>
public sealed class LedgerSubEntryViewModel(List<LedgerItem> entries)
{
    public string MaterialName { get; set; } = entries.First().MaterialEnum.ToString();
    public string Count { get; set; } = entries.Count.ToString();
    List<LedgerItem> Items { get; set; } = entries;

    public string AverageQuality
    {
        get
        {
            QualityEnum avgQualityEnum = (QualityEnum)Items.Average(i => (decimal)i.QualityEnum);

            return avgQualityEnum.ToString();
        }
    }
}