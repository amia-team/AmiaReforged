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
                        // Close button row
                        new NuiRow
                        {
                            Children =
                            {
                                new NuiSpacer { Width = 1f },
                                new NuiImage("ui_dash_close")
                                {
                                    Id = "btn_close",
                                    Width = 25f,
                                    Height = 25f,
                                    Tooltip = "Close Dashboard"
                                }
                            }
                        },
                        // Row 1: Rest, Pray, Emotes, Hide
                        new NuiRow
                        {
                            Children =
                            {
                                new NuiImage("ui_dash_rest")
                                {
                                    Id = "btn_rest",
                                    Width = 50f,
                                    Height = 50f,
                                    Tooltip = "Character Rest"
                                },
                                new NuiImage("ui_dash_pray")
                                {
                                    Id = "btn_pray",
                                    Width = 50f,
                                    Height = 50f,
                                    Tooltip = "Character Prayer"
                                },
                                new NuiImage("ui_dash_emote")
                                {
                                    Id = "btn_emotes",
                                    Width = 50f,
                                    Height = 50f,
                                    Tooltip = "Perform an Emote"
                                },
                                new NuiImage("ui_dash_hide")
                                {
                                    Id = "btn_hide",
                                    Width = 50f,
                                    Height = 50f,
                                    Tooltip = "Hide Equipment"
                                }
                            }
                        },

                        // Row 2: Player Tools, Utilities
                        new NuiRow
                        {
                            Children =
                            {
                                new NuiImage("ui_dash_tools")
                                {
                                    Id = "btn_player_tools",
                                    Width = 50f,
                                    Height = 50f,
                                    Tooltip = "Open Player Tools"
                                },
                                new NuiImage("ui_dash_util")
                                {
                                    Id = "btn_utilities",
                                    Width = 50f,
                                    Height = 50f,
                                    Tooltip = "Open Other Utilities"
                                }
                            }
                        }
                    ]
                },
            ]
        };
        return root;
    }
}
