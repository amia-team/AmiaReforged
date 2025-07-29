using AmiaReforged.Core.Models;
using AmiaReforged.Core.Services;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.DungeonMaster.EncounterBuilder.EncounterMaker;

public class EncounterMakerModel(NwPlayer player)
{
    [Inject] private Lazy<EncounterService> EncounterService { get; set; } = null!;
    public string EncounterName { get; set; } = String.Empty;

    public void CreateNewEncounter(string encounterName)
    {
        Encounter encounter = new()
        {
            Name = encounterName,
            DmId = player.CDKey
        };
        
        EncounterService.Value.AddEncounter(encounter);
    }
}