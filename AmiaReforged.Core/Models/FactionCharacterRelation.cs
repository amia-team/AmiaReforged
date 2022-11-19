using System.ComponentModel.DataAnnotations;
using AmiaReforged.Core.Entities;

namespace AmiaReforged.Core.Models;

public class FactionCharacterRelation
{
    [Key] public int Id { get; set; }
    public Guid CharacterId { get; set; }
    public string FactionName { get; set; } = null!;
    [Range(-100, 100)] public int Relation { get; set; }
}