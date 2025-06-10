using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.Core.Models;

public class StoredItem
{
    [Key] public long ItemId { get; set; }
    public required Guid PlayerCharacterId { get; set; }
    public required string ItemJson { get; set; }

    [ForeignKey("PlayerCharacterId")] public PlayerCharacter? Character { get; set; }
}