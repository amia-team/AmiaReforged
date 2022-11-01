using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace AmiaReforged.Core.Models;

public class FactionRelation
{
    
    [Key]
    public Guid Id { get; set; }
    
    [ForeignKey("Faction")]
    public string FactionName { get; set; } = null!;
    
    public string TargetFactionName { get; set; } = null!;

    [Range(-100, 100)]
    public int Relation { get; set; }
}