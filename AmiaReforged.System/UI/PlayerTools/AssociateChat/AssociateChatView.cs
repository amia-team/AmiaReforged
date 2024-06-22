using AmiaReforged.Core.UserInterface;
using Anvil.API;

namespace AmiaReforged.System.UI.PlayerTools.AssociateChat;

public class AssociateChatView : WindowView<AssociateChatView>
{
    public override string Id => "playertools.associatechat";
    public override string Title => "Associate Chat";
    public override bool ListInPlayerTools => true;

    public override NuiWindow? WindowTemplate { get; }
    public override IWindowController? CreateDefaultController(NwPlayer player)
    {
        return CreateController<AssociateChatController>(player);    
    }
}