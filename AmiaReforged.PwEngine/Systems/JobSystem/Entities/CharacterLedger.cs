using System.Text;
using AmiaReforged.PwEngine.Systems.JobSystem.Nui.ViewModels;

namespace AmiaReforged.PwEngine.Systems.JobSystem.Entities;

public class CharacterLedger(CharacterDataSource characterDataSource) : Ledger(characterDataSource)
{
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