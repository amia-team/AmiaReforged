using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.System.Services;

[ServiceBinding(typeof(CharacterService))]
public class CharacterService {
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /**
    * Class CharacterService
    * Once the character is created, create a reference 
    */
    public CharacterService() {
        Log.Info("Character Service initialized.");
        
    }

    public void StoreCharacter(String PCKey, String CDKey) {
        // We'll be storing:
        // PCKey (that little wand in your inventory)
        // CDKey
        // Character Name
        // 
    }
    
}