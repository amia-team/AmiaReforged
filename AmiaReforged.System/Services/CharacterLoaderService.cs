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
    private const string TravelAgencyTag = "core_travelroom";
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly CharacterService _characterService;

    public CharacterLoaderService(CharacterService characterService)
    {
        _characterService = characterService;

        if (RegisterToTravelAgency())
        {
            Log.Error("CharacterLoaderService initalization failed.");
            return;
        }

        Log.Info("Character Service initialized.");
    }

    private bool RegisterToTravelAgency()
    {
        NwArea? travelAgency = NwObject.FindObjectsWithTag<NwArea>(TravelAgencyTag).FirstOrDefault();
        if (travelAgency is null)
        {
            Log.Error("Something is very wrong, entry gate could not be found");
            return true;
        }

        travelAgency.OnEnter += StoreCharacter;
        return false;
    }

    private async void StoreCharacter(AreaEvents.OnEnter obj)
    {
        if (!obj.EnteringObject.IsPlayerControlled(out NwPlayer? player)) return;
        if (player.IsDM) return;
        if (player.IsPlayerDM) return;
        
        Log.Info($"Storing character: {player.LoginCreature?.Name}");
        NwItem? pcKey = player.LoginCreature?.FindItemWithTag("ds_pckey");
        if (pcKey is null) return;
        
        Log.Info("PCKey not null");

        string dbToken = pcKey.Name.Split("_")[1];
        if(!Guid.TryParse(dbToken, out Guid pcKeyGuid)) return;
        
        Log.Info($"Parsed GUID from PC Key: {pcKeyGuid.ToString()}");

        
        bool characterExists = await _characterService.CharacterExists(pcKeyGuid);

        NwTask.SwitchToMainThread();

        if (characterExists) return;
        
        string? playerFirstName = player.LoginCreature?.OriginalFirstName;
        string? playerLastName = player.LoginCreature?.OriginalLastName;
        string cdKey =  pcKey.Name.Split("_")[0];
        
        PlayerCharacter playerCharacter = new()
        {
            Id = pcKeyGuid,
            PlayerId = cdKey,
            FirstName = playerFirstName,
            LastName = playerLastName,
        };
        
        Log.Info($"Adding character: {playerCharacter.FirstName} {playerCharacter.LastName} ({playerCharacter.PlayerId}) with GUID: {playerCharacter.Id.ToString()}");
        
        await _characterService.AddCharacter(playerCharacter);
        
        await NwTask.SwitchToMainThread();
    }
}