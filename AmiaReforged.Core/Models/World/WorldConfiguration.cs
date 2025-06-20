using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.Core.Models.World;

public class WorldConfiguration
{
    [Key] public required string Key { get; init; }
    public required string Value { get; set; }
    public required string ValueType { get; init; }
}