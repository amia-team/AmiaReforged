using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Anvil.API;

namespace AmiaReforged.Core.Models;

public class StoredItem
{
    [Key] public long ItemId { get; set; }
    public Guid PlayerCharacterId { get; set; }
    public string ItemJson { get; set; }
    
    [ForeignKey("PlayerCharacterId")] public PlayerCharacter Character { get; set; }
}