namespace AmiaReforged.Core.Models.Sailing;

public sealed class SavedChartDiscovery
{
    public int Id { get; set; }
    public required string AccountKey { get; set; }
    public required string AreaResRef { get; set; }
    /// <summary>Exactly 256 bits in x-major order; zero is undiscovered.</summary>
    public required string Cells { get; set; }
    public bool IsValid() => !string.IsNullOrWhiteSpace(AccountKey) && !string.IsNullOrWhiteSpace(AreaResRef) &&
        Cells != null && Cells.Length == 256 && Cells.All(c => c is '0' or '1');
}
