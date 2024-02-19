using AmiaReforged.Core.UserInterface;
using Anvil.API;

namespace AmiaReforged.System.UI.PlayerTools.Spellbook;

public sealed class SpellbookView : WindowView<SpellbookView>
{
    public override string Id => "spellbook";
    public override string Title => "Spellbooks";
    public override NuiWindow? WindowTemplate { get; }
    public override IWindowController? CreateDefaultController(NwPlayer player)
    {
        return CreateController<SpellbookController>(player);
    }
}