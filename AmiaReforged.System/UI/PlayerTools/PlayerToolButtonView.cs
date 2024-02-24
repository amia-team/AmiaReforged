using AmiaReforged.Core.UserInterface;
using Anvil.API;

namespace AmiaReforged.System.UI.PlayerTools;

public class PlayerToolButtonView : WindowView<PlayerToolButtonView>
{
    public override string Id => "playertools.button";
    public override string Title => "Player Tools";

    public override bool ListInPlayerTools => false;

    public override IWindowController? CreateDefaultController(NwPlayer player)
    {
        return CreateController<PlayerToolButtonController>(player);
    }

    public override NuiWindow? WindowTemplate { get; }

    public readonly NuiButton Button;

    public readonly NuiBind<NuiRect> ButtonGeometry = new("btn_geo");

    public PlayerToolButtonView()
    {
        NuiRow root = new()
        {
            Children = new List<NuiElement>()
            {
                new NuiButton("Player Tools")
                {
                    Id = "ptools_open",
                    Width = 112f,
                    Height = 37f
                }.Assign(out Button)
            }
        };

        WindowTemplate = new NuiWindow(root, string.Empty)
        {
            Geometry = ButtonGeometry,
            Border = false,
            Closable = false,
            Transparent = true,
            Resizable = false,
            Collapsed = false,
        };
    }
}