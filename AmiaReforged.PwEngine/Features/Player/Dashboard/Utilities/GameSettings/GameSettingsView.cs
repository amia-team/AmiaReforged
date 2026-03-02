using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Utilities.GameSettings;

public sealed class GameSettingsView : IScryView
{
    // Scale factor for GUI scaling compensation (1.0 = no scaling)
    private float _scaleFactor = 1.0f;

    // Base sizes for elements (at 100% GUI scale)
    private const float BaseButtonSize = 40f;

    public NuiBind<string> XpBlockTooltip { get; } = new("xp_block_tooltip");

    /// <summary>
    /// Sets the scale factor for GUI scaling compensation.
    /// Call this before creating the window.
    /// </summary>
    public void SetScaleFactor(float scaleFactor)
    {
        _scaleFactor = scaleFactor;
    }

    public NuiLayout RootLayout()
    {
        // Calculate scaled sizes - divide by scale factor to compensate for NWN's GUI scaling
        float buttonSize = BaseButtonSize / _scaleFactor;

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
                            Width = buttonSize,
                            Height = buttonSize,
                            ImageAspect = NuiAspect.Fit
                        },
                        new NuiImage("ui_util_symbol")
                        {
                            Id = "btn_emote_symbol",
                            Tooltip = "Change Emote Symbol",
                            Width = buttonSize,
                            Height = buttonSize,
                            ImageAspect = NuiAspect.Fit
                        },
                        new NuiImage("ui_util_party")
                        {
                            Id = "btn_party_advertiser",
                            Tooltip = "Party Advertiser",
                            Width = buttonSize,
                            Height = buttonSize,
                            ImageAspect = NuiAspect.Fit
                        },
                        new NuiImage("ui_util_pvp")
                        {
                            Id = "btn_pvp_tool",
                            Tooltip = "PvP Settings",
                            Width = buttonSize,
                            Height = buttonSize,
                            ImageAspect = NuiAspect.Fit
                        }
                    }
                }
            }
        };
        return root;
    }
}
