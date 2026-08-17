namespace AmiaReforged.Core.Models.Sailing;

public class SailingArea
{
    public required string AreaResRef { get; set; }

    public float MinX { get; set; }
    public float MaxX { get; set; }

    public float MinY { get; set; }
    public float MaxY { get; set; }

    public string? NorthAreaResRef { get; set; }
    public string? SouthAreaResRef { get; set; }
    public string? EastAreaResRef { get; set; }
    public string? WestAreaResRef { get; set; }

    public SailingLocation? NorthEntry { get; set; }
    public SailingLocation? SouthEntry { get; set; }
    public SailingLocation? EastEntry { get; set; }
    public SailingLocation? WestEntry { get; set; }
}