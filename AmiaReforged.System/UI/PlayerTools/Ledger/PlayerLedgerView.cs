using AmiaReforged.Core.UserInterface;
using Anvil.API;

namespace AmiaReforged.System.UI.PlayerTools.Ledger;

public class PlayerLedgerView : WindowView<PlayerLedgerView>
{
    public sealed override string Id => "playertools.ledger";
    public sealed override string Title => "Ledger of Goods";
    public override NuiWindow? WindowTemplate { get; }

    public readonly NuiBind<string> Search = new("search_val");
    public readonly NuiBind<string> ItemNames = new("item_names");
    public readonly NuiBind<string> ItemIds = new("item_ids");
    public readonly NuiBind<int> ItemCount = new("item_count");


    public override IWindowController? CreateDefaultController(NwPlayer player)
    {
        return CreateController<PlayerLedgerController>(player);
    }

    public PlayerLedgerView()
    {
        List<NuiListTemplateCell> rowTemplate = new()
        {
        };

        NuiColumn root = new()
        {
        };

        WindowTemplate = new NuiWindow(root, Title);
    }
}