using Anvil.API;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;

namespace AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge;

/// <summary>
/// Custom view for the over-budget warning popup in Mythal Forge.
/// </summary>
public sealed class OverBudgetWarningView : ScryView<OverBudgetWarningPresenter>
{
    public OverBudgetWarningView(NwPlayer player)
    {
        Presenter = new OverBudgetWarningPresenter(player, this);
    }

    public override OverBudgetWarningPresenter Presenter { get; protected set; }

    public override NuiLayout RootLayout()
    {
        NuiColumn layout = new()
        {
            Width = 480f,
            Height = 210f,
            Children =
            {
                // Background image
                new NuiRow
                {
                    Width = 0f,
                    Height = 0f,
                    DrawList = new()
                    {
                        new NuiDrawListImage("ui_bg", new NuiRect(-20f, -20f, 520f, 290f))
                    }
                },
                new NuiSpacer { Height = 5f },
                new NuiLabel("===WARNING: Item Over Budget!!!===") { Width = 450f, Height = 22f, HorizontalAlign = NuiHAlign.Center, ForegroundColor = new Color(100, 32, 32) },
                new NuiLabel("This item is stronger than what a Mythal Forge can create!")
                    { Width = 450f, Height = 20f, HorizontalAlign = NuiHAlign.Center, ForegroundColor = new Color(30, 20, 12) },
                new NuiLabel("Take care not to weaken the item when editing it!")
                    { Width = 450f, Height = 18f, HorizontalAlign = NuiHAlign.Center, ForegroundColor = new Color(30, 20, 12) },
                new NuiSpacer { Height = 15f },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 130f },
                        new NuiButton("CONFIRM") { Id = "ok_button", Width = 150f, Height = 38f, Encouraged = true }
                    }
                }
            }
        };

        return layout;
    }
}

