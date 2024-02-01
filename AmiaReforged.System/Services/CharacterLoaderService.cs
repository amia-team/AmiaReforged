using AmiaReforged.Core.Models;
using AmiaReforged.Core.Services;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.System.Services;

[ServiceBinding(typeof(CharacterLoaderService))]
public class CharacterLoaderService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly CharacterService _characterService;

    public CharacterLoaderService(CharacterService characterService)
    {
        _characterService = characterService;

        if (RegisterToEntryStatue())
        {
            Log.Error("CharacterLoaderService initalization failed.");
            return;
        }

        Log.Info("Character Service initialized.");
    }

    private bool RegisterToEntryStatue()
    {
        NwPlaceable? entryStatue = NwObject.FindObjectsWithTag<NwPlaceable>("ds_entrygate").FirstOrDefault();
        if (entryStatue is null)
        {
            Log.Error("Something is very wrong, entry gate could not be found");
            return true;
        }

        entryStatue.OnUsed += StoreCharacter;
        return false;
    }

    private async void StoreCharacter(PlaceableEvents.OnUsed obj)
    {
        if (!obj.UsedBy.IsPlayerControlled(out NwPlayer? player)) return;
        if (player.LoginCreature is null) return;
        
        string dbToken = player.LoginCreature!.Inventory.Items.Where(i => i.Tag == "ds_pckey").First().ToString().Split("_")[1];
        bool characterExists = await _characterService.CharacterExists(Guid.Parse(dbToken));
        
        if(characterExists) return;
        
        NwTask.SwitchToMainThread();
        await AddCharacterToDatabase(player);
    }
    private async Task AddCharacterToDatabase(NwPlayer player)
    {
        string dbToken = player.LoginCreature!.Inventory.Items.Where(i => i.Tag == "ds_pckey").First().ToString().Split("_")[1];

        PlayerCharacter playerCharacter = new()
        {
            Id = Guid.Parse(dbToken),
            CdKey = player.CDKey,
            FirstName = player.LoginCreature.OriginalFirstName,
            LastName = player.LoginCreature.OriginalLastName,
        };
        
        await _characterService.AddCharacter(playerCharacter);
    }
}