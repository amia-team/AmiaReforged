using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Systems.WindowingSystem;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge;

public sealed class MythalForgeView : NuiView<MythalForgeView>
{
    public override string Id => "crafting.mythal_forge";
    public override string Title => "Mythal Forge";
    public override NuiWindow? WindowTemplate { get; }

    public readonly NuiButtonImage SelectItemButton;

    public override INuiController? CreateDefaultController(NwPlayer player)
    {
        return CreateController<MythalForgeController>(player);
    }

    public MythalForgeView()
    {
        NuiColumn rootElement = new()
        {
            Children =
            {
                new NuiRow
                {
                    Children =
                    {
                        new NuiLabel("Select an item to craft:"),
                        new NuiButtonImage("ir_sell02")
                        {
                            Aspect = 1f,
                            Tooltip = "Select Item"
                        }.Assign(out SelectItemButton)
                    }
                }
            }
        };

        WindowTemplate = new NuiWindow(rootElement, Title)
        {
            Border = false,
            Transparent = true,
            Resizable = false,
            Collapsed = false,
        };
    }
}