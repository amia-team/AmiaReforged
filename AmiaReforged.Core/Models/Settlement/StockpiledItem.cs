using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.Core.Models.Settlement;

public class StockpiledItem
{
    [Key] public required long Id { get; set; }
   
    public Stockpile Stockpile { get; set; }
    [ForeignKey("Stockpile")] public long StockpileId { get; set; }
    
    public Guid AddedBy { get; set; }
    [ForeignKey("AddedBy")] public required PlayerCharacter AddedByCharacter { get; set; }
    
    public EconomyItem Item { get; set; }
    [ForeignKey("Item")] public long ItemId { get; set; }
}
