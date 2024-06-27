using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.Core.Models.Faction;

public class FactionCharacterRelation
{
    [Key] public long Id { get; set; }
    public Guid CharacterId { get; set; }
    [ForeignKey("CharacterId")] public PlayerCharacter Character { get; set; } = null!;

    public string FactionName { get; set; } = null!;
    [ForeignKey("FactionName")] public FactionEntity Faction { get; set; } = null!;
    [Range(-100, 100)] public int Relation { get; set; }
}