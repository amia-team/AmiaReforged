using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.Core.Models.Settlement;

public class Stockpile
{
    [Key] public long Id { get; set; }
    
    public required string Name { get; set; }
    public required List<StockpiledItem> ItemData { get; set; }
    
    public required List<StockpileUser> Characters { get; set; }
}