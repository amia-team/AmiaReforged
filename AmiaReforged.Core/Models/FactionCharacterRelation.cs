using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AmiaReforged.Core.Entities;

namespace AmiaReforged.Core.Models;

public class FactionCharacterRelation
{
    [ForeignKey("Character")]
    public Guid CharacterId { get; set; }
    [ForeignKey("Faction")] public string FactionName { get; set; } = null!;
    [Range(-100, 100)] public int Relation { get; set; }
}