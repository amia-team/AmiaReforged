using AmiaReforged.Core;
using AmiaReforged.Core.Entities;
using AmiaReforged.Core.Services;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.System.Services;

// [ServiceBinding(typeof(CharacterLoaderService))]
public class CharacterLoaderService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private const string DatabaseToken = "db_token";
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

        bool playerHasDatabaseToken = player.LoginCreature!.Inventory.Items.Any(i => i.Tag == DatabaseToken);
        if (playerHasDatabaseToken) return;

        await AddTokenToCharacter(player);
        NwTask.SwitchToMainThread();
        AddCharacterToDatabase(player);
    }

    private static async Task AddTokenToCharacter(NwPlayer player)
    {
        NwItem? item = await NwItem.Create(DatabaseToken, player.LoginCreature);
        NwTask.SwitchToMainThread();
        if (item is null)
        {
            Log.Error($"Could not create database token for {player.LoginCreature?.Name}.");
            player.SendServerMessage(
                "Well, this is embarrassing. We couldn't create your database token.");
            return;
        }
        item.Name = Guid.NewGuid().ToString();
    }

    private void AddCharacterToDatabase(NwPlayer player)
    {
        string dbToken = player.LoginCreature!.Inventory.Items.Where(i => i.Tag == "db_token").First().Name;

        Character character = new()
        {
            Id = Guid.Parse(dbToken),
            CdKey = player.CDKey,
            FirstName = player.LoginCreature.OriginalFirstName,
            LastName = player.LoginCreature.OriginalLastName,
            IsPlayerCharacter = true
        };
        
        _characterService.AddCharacter(character);
    }
}