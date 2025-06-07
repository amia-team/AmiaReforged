using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Anvil.API;
using NWN.Core;

namespace AmiaReforged.Core.Models;

public class Encounter
{
    [Key] public long Id { get; set; }
    
    public required string Name { get; set; }
    
    public int EncounterSize { get; set; }
    
    public List<EncounterEntry> EncounterEntries { get; set; }
    
    public required string DmId { get; set; }
    [ForeignKey("DmId")] public Dm Dm { get; set; }

    public void SpawnEncounters(Location location)
    {
        foreach (EncounterEntry entry in EncounterEntries)
        {
            NwCreature? creature = NwCreature.Deserialize(entry.SerializedString);
            if(creature is null) continue;
            
            NWScript.CopyObject(creature, location);
        }
    }
}   