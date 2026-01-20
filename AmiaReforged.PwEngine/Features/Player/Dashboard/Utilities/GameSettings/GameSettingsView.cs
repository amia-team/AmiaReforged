using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Utilities.GameSettings;

public sealed class GameSettingsView : IScryView
{
    public NuiBind<string> XpBlockTooltip { get; } = new("xp_block_tooltip");

    public NuiLayout RootLayout()
    {
        NuiColumn root = new()
        {
            Children = new List<NuiElement>
            {
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiImage("ui_util_xp")
                        {
                            Id = "btn_xp_block",
                            Tooltip = XpBlockTooltip,
                            Width = 40f,
                            Height = 40f,
                            ImageAspect = NuiAspect.Fit
                        },
                        new NuiImage("ui_util_symbol")
                        {
                            Id = "btn_emote_symbol",
                            Tooltip = "Change Emote Symbol",
                            Width = 40f,
                            Height = 40f,
                            ImageAspect = NuiAspect.Fit
                        },
                        new NuiImage("ui_util_party")
                        {
                            Id = "btn_party_advertiser",
                            Tooltip = "Party Advertiser",
                            Width = 40f,
                            Height = 40f,
                            ImageAspect = NuiAspect.Fit
                        },
                        new NuiImage("ui_util_pvp")
                        {
                            Id = "btn_pvp_tool",
                            Tooltip = "PvP Settings",
                            Width = 40f,
                            Height = 40f,
                            ImageAspect = NuiAspect.Fit
                        }
                    }
                }
            }
        };
        return root;
    }
}
