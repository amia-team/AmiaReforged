using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.Core.Models;

public class Faction
{
    [Key]
    public string Name { get; set; } = null!;

    [Required]
    public string Description { get; set; } = null!;
    
    public List<Guid> Members { get; set; } = null!;
}