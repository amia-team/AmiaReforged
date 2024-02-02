using AmiaReforged.Core.Models;
using AmiaReforged.Core.Services;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.System.Services;

[ServiceBinding(typeof(CharacterListLayout))]
public class CharacterListLayout
{
    private readonly PlayerDataService _dataService;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public CharacterListLayout(PlayerDataService dataService)
    {
        _dataService = dataService;
        
        Log.Info("CharacterListLayout initialized.");
    }
    
    public async Task OpenCharacterListWindow(ModuleEvents.OnNuiEvent nuiEvent)
    {
        IEnumerable<PlayerCharacter> characters = await _dataService.GetPlayerCharacters(nuiEvent.Player.CDKey);

        IEnumerable<PlayerCharacter> playerCharacters = characters.ToList();
        Log.Info("Player characters retrieved.");
        if (playerCharacters.ToList().Count == 0) return;
        
        List<NuiLabel> characterLabels = playerCharacters.Select(character => new NuiLabel(character.FirstName)).ToList();
        
        NuiColumn root = new()
        {
            Children = new List<NuiElement>
            {
                new NuiRow
                {
                    Children = new List<NuiElement>(characterLabels)
                }
            }
        };
        
        NuiWindow window = new(root, "Characters")
        {
            Closable = true,
            Geometry = new NuiRect(500f, 100f, 300f, 400f)
        };
        
        nuiEvent.Player.TryCreateNuiWindow(window, out NuiWindowToken token);
    }
}