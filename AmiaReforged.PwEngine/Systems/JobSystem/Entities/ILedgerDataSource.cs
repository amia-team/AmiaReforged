using AmiaReforged.PwEngine.Systems.JobSystem.Nui.ViewModels;

namespace AmiaReforged.PwEngine.Systems.JobSystem.Entities;

public interface ILedgerDataSource
{
    List<LedgerEntry> LoadEntries();
}