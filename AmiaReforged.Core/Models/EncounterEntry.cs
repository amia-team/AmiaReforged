using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.Core.Models;

public class EncounterEntry
{
    [Key] public long Id { get; set; }
    [Required] public byte[] SerializedString { get; set; }
    public string Name { get; set; }
    
    public long EncounterId { get; set; }
    [ForeignKey("EncounterId")] public Encounter Encounter { get; set; }    
}