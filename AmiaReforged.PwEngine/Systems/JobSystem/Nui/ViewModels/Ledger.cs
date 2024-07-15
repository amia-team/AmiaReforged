namespace AmiaReforged.PwEngine.Systems.JobSystem.Nui.ViewModels;

public class Ledger
{
    public List<long> ItemReferences { get; set; }
    public List<LedgerEntry> Entries { get; set; }

    public int TotalValue => Entries.Sum(e => e.TotalValue);
}