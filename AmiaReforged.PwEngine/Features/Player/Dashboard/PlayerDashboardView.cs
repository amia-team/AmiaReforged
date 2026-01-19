using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard;

public sealed class PlayerDashboardView : ScryView<PlayerDashboardPresenter>
{
    public override PlayerDashboardPresenter Presenter { get; protected set; }

    public PlayerDashboardView(NwPlayer player)
    {
        Presenter = new PlayerDashboardPresenter(this, player);
        // Injection is handled by PlayerDashboardService.OpenDashboard()
    }

    public override NuiLayout RootLayout()
    {
        NuiColumn root = new()
        {
            Children =
            [
                new NuiColumn
                {
                    Children =
                    [
                        new NuiRow
                        {
                            Children =
                            {
                                new NuiImage("ui_dash_rest")
                                {
                                    Id = "btn_rest",
                                    Width = 40f,
                                    Height = 40f,
                                    ImageAspect = NuiAspect.Fit,
                                    Tooltip = "Character Rest"
                                },
                                new NuiImage("ui_dash_pray")
                                {
                                    Id = "btn_pray",
                                    Width = 40f,
                                    Height = 40f,
                                    ImageAspect = NuiAspect.Fit,
                                    Tooltip = "Character Prayer"
                                },
                                new NuiImage("ui_dash_emote")
                                {
                                    Id = "btn_emotes",
                                    Width = 40f,
                                    Height = 40f,
                                    ImageAspect = NuiAspect.Fit,
                                    Tooltip = "Perform an Emote"
                                },
                                new NuiImage("ui_dash_hide")
                                {
                                    Id = "btn_hide",
                                    Width = 40f,
                                    Height = 40f,
                                    ImageAspect = NuiAspect.Fit,
                                    Tooltip = "Hide Equipment"
                                },
                                new NuiImage("ui_dash_util")
                                {
                                    Id = "btn_utilities",
                                    Width = 40f,
                                    Height = 40f,
                                    ImageAspect = NuiAspect.Fit,
                                    Tooltip = "Open Other Utilities"
                                },
                                new NuiImage("ui_dash_tools")
                                {
                                    Id = "btn_player_tools",
                                    Width = 40f,
                                    Height = 40f,
                                    ImageAspect = NuiAspect.Fit,
                                    Tooltip = "Open Player Tools"
                                }
                            }
                        },
                        new NuiImage("ui_dash_close")
                        {
                            Id = "btn_close",
                            Width = 15f,
                            Height = 15f,
                            ImageAspect = NuiAspect.Fit,
                            Tooltip = "Close Dashboard"
                        }
                    ]
                }
            ]
        };
        return root;
    }
}
