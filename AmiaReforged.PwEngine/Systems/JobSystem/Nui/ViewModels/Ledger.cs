using System.Text;

namespace AmiaReforged.PwEngine.Systems.JobSystem.Nui.ViewModels;

public class Ledger
{
    public List<long> ItemReferences { get; set; }
    public List<LedgerEntry> Entries { get; set; }

    public int TotalValue => Entries.Sum(e => e.TotalValue);

    // to string method
    public override string ToString()
    {
        StringBuilder sb = new();
        sb.AppendLine($"Total Value: {TotalValue}");
        foreach (LedgerEntry entry in Entries)
        {
            sb.AppendLine(entry.ToString());
        }

        return sb.ToString();
    }
}