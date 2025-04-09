namespace AmiaReforged.PwEngine.Systems.JobSystem.Entities;

public class Ledger
{
    public List<LedgerEntry> Entries { get; init; }
    public int TotalValue => Entries.Sum(e => e.TotalValue);

    public Ledger()
    {
        // Empty for allowing in place initialization
    }

    /// <summary>
    /// Load a ledger from a data source
    /// </summary>
    /// <param name="dataSource"></param>
    public Ledger(ILedgerDataSource dataSource)
    {
        Entries = dataSource.LoadEntries();
    }
}