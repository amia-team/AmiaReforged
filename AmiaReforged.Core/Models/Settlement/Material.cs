using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.Core.Models.Settlement;

public class Material
{
    [Key] public int Id { get; set; }
    [MaxLength(120)] public required string Name { get; set; }
    public MaterialType Type { get; set; }
    public float ValueModifier { get; set; }
    public float MagicModifier { get; set; }
    public float DurabilityModifier { get; set; }
}
