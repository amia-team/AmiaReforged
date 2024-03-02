using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.Core.Models.Faction;

public class FactionCharacterRelation
{
    [ForeignKey("Character")]
    public Guid CharacterId { get; set; }
    [ForeignKey("Faction")] public string FactionName { get; set; } = null!;
    [Range(-100, 100)] public int Relation { get; set; }
}