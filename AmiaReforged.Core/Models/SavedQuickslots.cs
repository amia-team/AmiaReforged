using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.Core.Models;

public class SavedQuickslots
{
    [Key] public long Id { get; set; }
    
    public Guid PlayerCharacterId { get; set; }
    
    [ForeignKey("PlayerCharacterId")] public PlayerCharacter PlayerCharacter { get; set; }

    public string Name { get; set; }
    
    public byte[] Quickslots { get; set; }
}