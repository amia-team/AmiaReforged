using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.Core.Models.Settlement;

public class StockpileUser
{
    [Key] public long Id { get; set; }
    public Guid CharacterId { get; set; }
    public long StockpileId { get; set; }
    
    [ForeignKey("CharacterId")] public required PlayerCharacter Character { get; set; }
    [ForeignKey("StockpileId")] public required Stockpile Stockpile { get; set; }
}