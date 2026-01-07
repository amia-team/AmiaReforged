using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.CharacterArchive;

/// <summary>
/// Tool window entry for the Character Archive/Vault Manager.
/// Allows players to move characters between their active vault and archive storage.
/// </summary>
public class CharacterArchiveToolView : IToolWindow
{
    public CharacterArchiveToolView(NwPlayer player)
    {
        // Constructor required by IToolWindow pattern but not used
    }

    public string Id => "playertools.characterarchive";
    public bool ListInPlayerTools => true;
    public bool RequiresPersistedCharacter => false;
    public string Title => "Character Archive Manager";
    public string CategoryTag => "Character";

    public IScryPresenter ForPlayer(NwPlayer player)
    {
        // Get the service from DI
        CharacterArchiveService? service = AnvilCore.GetService<CharacterArchiveService>();
        if (service == null)
        {
            throw new InvalidOperationException("CharacterArchiveService not found in DI container");
        }

        // Create a new presenter instance for this player
        return new CharacterArchiveScryPresenter(player, service);
    }
}

