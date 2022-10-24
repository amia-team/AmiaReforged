using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.System.Services;

[ServiceBinding(typeof(CharacterService))]
public class CharacterService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();


    // Take a store
    // When a character hasn't been added to the store, add them to the store and make sure that they can be referenced by their PC key. Make sure they have their player's public cd key referenced in the data as well.

    public CharacterService()
    {
        NwPlaceable? entryStatue = NwObject.FindObjectsWithTag<NwPlaceable>("ds_entrygate").FirstOrDefault();
        // Check that the entry statue is not "null".
        if (entryStatue == null)
        {
            Log.Error("Something is very wrong, entry gate could not be found");
            return;
        }

        entryStatue.OnUsed += StoreCharacter;
        Log.Info("Character Service initialized.");
    }

    private void StoreCharacter(PlaceableEvents.OnUsed obj)
    {
        if(!obj.UsedBy.IsPlayerControlled(out NwPlayer? player))
        {
            return;
        }

        if (player.LoginCreature == null) return;
        
        // Check if the player has a character already stored.

    }
}