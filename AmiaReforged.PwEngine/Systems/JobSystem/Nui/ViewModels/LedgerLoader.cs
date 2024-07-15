using AmiaReforged.PwEngine.Systems.JobSystem.Entities;

namespace AmiaReforged.PwEngine.Systems.JobSystem.Nui.ViewModels;

public class LedgerLoader
{
    public Ledger FromItemStorage(ItemStorage storage)
    {
        Ledger ledger = new()
        {
            Entries = new List<LedgerEntry>(),
            ItemReferences = new List<long>()
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
                Items = new List<LedgerItem>()
            };

            ledger.Entries.Add(entry);

            // Add individual items to the ledger entry
            foreach (StoredJobItem storedItem in storage.Items.Where(i => i.JobItem.Name == itemName))
            {
                entry.Items.Add(new LedgerItem
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