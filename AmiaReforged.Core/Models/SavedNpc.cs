using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.Core.Models;

public class SavedNpc
{
    [Key] public long Id { get; set; }

    public required string Name { get; set; }

    public required string Serialized { get; set; }

    public required string DmKey { get; set; }

    public bool AnyoneCanUse { get; set; }
}