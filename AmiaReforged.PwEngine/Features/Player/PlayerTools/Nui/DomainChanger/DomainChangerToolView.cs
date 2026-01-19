using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.DomainChanger;

/// <summary>
/// Tool window entry for the Domain Changer.
/// Allows clerics to change their domains with a 30-day cooldown.
/// </summary>
public class DomainChangerToolView : IToolWindow
{
    public DomainChangerToolView(NwPlayer player)
    {
        // Constructor required by IToolWindow pattern
    }

    public string Id => "playertools.domainchanger";
    public bool ListInPlayerTools => true;
    public bool RequiresPersistedCharacter => false;
    public string Title => "Domain Changer";
    public string CategoryTag => "Character";

    // Using default implementation from IToolWindow - always show the tool
    // Cleric validation will be handled inside the DomainChangerPresenter instead

    public IScryPresenter ForPlayer(NwPlayer player)
    {
        // Defensive check - should never happen but prevents crashes
        if (player == null)
        {
            throw new ArgumentNullException(nameof(player), "Cannot create Domain Changer for null player");
        }

        // Create a new presenter instance for this player
        DomainChangerView view = new();
        return new DomainChangerPresenter(view, player);
    }
}
