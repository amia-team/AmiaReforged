using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.Core.Models;

public class FactionRelation
{
    [Key]
    public Guid Id { get; set; }
    public Faction Faction { get; set; }
    public Faction TargetFaction { get; set; }
    [Range(-100, 100)]
    public int Relation { get; set; }
}