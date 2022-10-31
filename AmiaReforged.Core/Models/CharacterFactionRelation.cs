using System.ComponentModel.DataAnnotations;
using AmiaReforged.Core.Entities;

namespace AmiaReforged.Core.Models;

public class CharacterFactionRelation
{
    [Key]
    public int Id { get; set; }
    public AmiaCharacter Character { get; set; }
    public Faction Faction { get; set; }
    [Range(-100, 100)]
    public int Relation { get; set; }
}