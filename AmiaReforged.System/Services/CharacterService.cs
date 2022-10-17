using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.System.Services;

[ServiceBinding(typeof(CharacterService))]
public class CharacterService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /**
    * Class CharacterService
    * Once the character is created, create a reference 
    */
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
        string characterName = obj.UsedBy.Name;
    }
}