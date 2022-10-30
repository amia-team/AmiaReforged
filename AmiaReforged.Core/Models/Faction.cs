using System.ComponentModel.DataAnnotations;
using AmiaReforged.Core.Entities;

namespace AmiaReforged.Core.Models;

public class Faction
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public string Description { get; set; } = null!;

    [Required]
    public IEnumerable<AmiaCharacter> Members { get; set; } = null!;
}