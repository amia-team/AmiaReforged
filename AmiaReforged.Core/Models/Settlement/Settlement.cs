using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.Core.Models.Settlement;

public class Settlement
{
    [Key] public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required Stockpile Stockpile { get; set; }
    [ForeignKey("Stockpile")] public long StockpileId { get; set; }
}