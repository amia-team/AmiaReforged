using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
using AmiaReforged.PwEngine.Systems.JobSystem.Nui.ViewModels;
using AmiaReforged.PwEngine.Systems.JobSystem.Storage.Mapping;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.JobSystem.Nui;

[ServiceBinding(typeof(LedgerLoader))]
public class LedgerLoader(JobSystemMappingService mappingService)
{
    public Ledger FromItemStorage(ItemStorage storage)
    {
        Ledger ledger = new()
        {
            Entries = new(),
            ItemReferences = new()
        };

        foreach (StoredJobItem storedItem in storage.Items)
        {
            ledger.ItemReferences.Add(storedItem.Id);
        }

        string[] itemNames = storage.Items.Select(i => i.JobItem.Name).Distinct().ToArray();


        foreach (string itemName in itemNames)
        {
            LedgerEntry entry = new()
            {
                Name = itemName,
                Quantity = storage.Items.Count(i => i.JobItem.Name == itemName),
                AverageQuality = AverageQualityCalculator.Calculate(storage.Items.Where(i => i.JobItem.Name == itemName)
                    .Select(i => i.JobItem.Quality).ToArray()),
                BaseValue = storage.Items.First(i => i.JobItem.Name == itemName).JobItem.BaseValue,
                Items = new()
            };

            ledger.Entries.Add(entry);

            // Add individual items to the ledger entry
            foreach (StoredJobItem storedItem in storage.Items.Where(i => i.JobItem.Name == itemName))
            {
                // This is a bit of a hack, but it works for now
                entry.Type = storedItem.JobItem.Type;

                entry.Items.Add(new()
                {
                    Name = storedItem.JobItem.Name,
                    QualityEnum = storedItem.JobItem.Quality,
                    MaterialEnum = storedItem.JobItem.Material,
                    MagicModifier = storedItem.JobItem.MagicModifier,
                    DurabilityModifier = storedItem.JobItem.DurabilityModifier,
                    BaseValue = storedItem.JobItem.BaseValue
                });
            }
        }

        return ledger;
    }

    public Ledger FromPlayer(NwCreature tokenPlayer)
    {
        List<NwItem> nwItems = tokenPlayer.Inventory.Items.Where(i => i.ResRef.StartsWith(value: "js_")).ToList();

        ICollection<JobItem> jobItems = nwItems.Select(i => mappingService.MapFrom(i)).ToList();

        ICollection<JobItem> distinctItems = jobItems.DistinctBy(jb => jb.Name).ToList();

        Ledger ledger = new()
        {
            Entries = new(),
            ItemReferences = new()
        };

        foreach (JobItem item in distinctItems)
        {
            LedgerEntry entry = new()
            {
                Name = item.Name,
                Quantity = jobItems.Count(i => i.Name == item.Name),
                AverageQuality = AverageQualityCalculator.Calculate(jobItems.Where(i => i.Name == item.Name)
                    .Select(i => i.Quality).ToArray()),
                BaseValue = item.BaseValue,
                Items = new()
            };

            ledger.Entries.Add(entry);

            // Add individual items to the ledger entry
            foreach (JobItem jobItem in jobItems.Where(i => i.Name == item.Name))
            {
                // This is a bit of a hack, but it works for now
                entry.Type = jobItem.Type;
                entry.Items.Add(new()
                {
                    Name = jobItem.Name,
                    QualityEnum = jobItem.Quality,
                    MaterialEnum = jobItem.Material,
                    MagicModifier = jobItem.MagicModifier,
                    DurabilityModifier = jobItem.DurabilityModifier,
                    BaseValue = jobItem.BaseValue
                });

                // Add item reference to the ledger
                ledger.ItemReferences.Add(jobItem.Id);
            }
        }

        return ledger;
    }
}

public static class AverageQualityCalculator
{
    public static QualityEnum Calculate(QualityEnum[] qualities)
    {
        int totalQuality = qualities.Sum(quality => (int)quality);

        int averageQuality = totalQuality / qualities.Length;
        return (QualityEnum)averageQuality;
    }
}