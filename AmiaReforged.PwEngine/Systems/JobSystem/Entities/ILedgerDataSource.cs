using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions.Common;

namespace AmiaReforged.PwEngine.Systems.JobSystem.Entities;

public interface ILedgerDataSource
{
    List<LedgerEntry> LoadEntries();
}