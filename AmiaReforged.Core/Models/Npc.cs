using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.Core.Models;

public class Npc
{
    [Key] public long Id { get; set; }
    
    public required string Name { get; set; } 
    public required string DmCdKey { get; set; }
    public bool Public { get; set; }

    public required byte[] Serialized { get; set; }
}