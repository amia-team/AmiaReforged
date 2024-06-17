using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.Core.Models.Settlement;

[Index(nameof(Name), IsUnique = true)]
public class Material
{
    [Key] public int Id { get; set; }
    
    [MaxLength(120)]
    public string Name { get; set; }
    public MaterialType Type { get; set; }
    public float ValueModifier { get; set; }
    public float MagicModifier { get; set; }
    public float DurabilityModifier { get; set; }
}
