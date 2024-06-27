using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.Core.Models.Faction;

public class FactionCharacterRelation
{
    [Key] public long Id { get; init; }
    public Guid CharacterId { get; init; }
    [ForeignKey("CharacterId")] public PlayerCharacter Character { get; init; } = null!;

    public long FactionId { get; set; }
    [ForeignKey("FactionId")] public FactionEntity Faction { get; set; } = null!;
    
    [Range(-100, 100)] public int Relation { get; set; }
}