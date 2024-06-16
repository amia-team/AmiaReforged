using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.Core.Models.Settlement;

/// <summary>
///  Represents a single source of truth for all representations of game items in the economy. An instance of an item will refer to this by its Id.
/// </summary>
public class EconomyItem
{
    [Key] public long Id { get; set; }
    public string? Name { get; set; }
    public Material Material { get; set; }
    [ForeignKey("Material")] public int MaterialId { get; set; }
    public Quality Quality { get; set; }
    [ForeignKey("Quality")] public int QualityId { get; set; }
    public int BaseValue { get; set; }
}