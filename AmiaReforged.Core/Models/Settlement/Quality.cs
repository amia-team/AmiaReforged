using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.Core.Models.Settlement;

public class Quality
{
    [Key] public int Id { get; set; }
    [MaxLength(120)] public required string Name { get; set; }
    public float ValueModifier { get; set; }
}