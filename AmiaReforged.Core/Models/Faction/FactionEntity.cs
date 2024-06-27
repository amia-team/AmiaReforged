using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.Core.Models.Faction;

public class FactionEntity
{
    [Key] public long Id { get; set; }
    
    public string Name { get; set; } = null!;

    [Required]
    public string Description { get; set; } = null!;
    
    public List<PlayerFactionMember> PlayerMembers { get; set; } = null!;
}