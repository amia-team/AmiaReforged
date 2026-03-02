using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard;

public sealed class PlayerDashboardView : ScryView<PlayerDashboardPresenter>
{
    public override PlayerDashboardPresenter Presenter { get; protected set; }

    // Scale factor for GUI scaling compensation (1.0 = no scaling)
    private float _scaleFactor = 1.0f;

    // Base sizes for elements (at 100% GUI scale)
    private const float BaseButtonSize = 40f;
    private const float BaseCloseButtonSize = 15f;

    // Bind for collision bubble tooltip (changes based on current state)
    public readonly NuiBind<string> BubbleTooltip = new("bubble_tooltip");

    public PlayerDashboardView(NwPlayer player)
    {
        Presenter = new PlayerDashboardPresenter(this, player);
        // Injection is handled by PlayerDashboardService.OpenDashboard()
    }

    /// <summary>
    /// Sets the scale factor for GUI scaling compensation.
    /// Call this before creating the window.
    /// </summary>
    public void SetScaleFactor(float scaleFactor)
    {
        _scaleFactor = scaleFactor;
    }

    public override NuiLayout RootLayout()
    {
        // Calculate scaled sizes - divide by scale factor to compensate for NWN's GUI scaling
        float buttonSize = BaseButtonSize / _scaleFactor;
        float closeButtonSize = BaseCloseButtonSize / _scaleFactor;

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
                                    Width = buttonSize,
                                    Height = buttonSize,
                                    ImageAspect = NuiAspect.Fit,
                                    Tooltip = "Character Rest"
                                },
                                new NuiImage("ui_dash_pray")
                                {
                                    Id = "btn_pray",
                                    Width = buttonSize,
                                    Height = buttonSize,
                                    ImageAspect = NuiAspect.Fit,
                                    Tooltip = "Character Prayer"
                                },
                                new NuiImage("ui_dash_emote")
                                {
                                    Id = "btn_emotes",
                                    Width = buttonSize,
                                    Height = buttonSize,
                                    ImageAspect = NuiAspect.Fit,
                                    Tooltip = "Perform an Emote"
                                },
                                new NuiImage("ui_dash_hide")
                                {
                                    Id = "btn_hide",
                                    Width = buttonSize,
                                    Height = buttonSize,
                                    ImageAspect = NuiAspect.Fit,
                                    Tooltip = "Hide Equipment"
                                },
                                new NuiImage("ui_util_bubble")
                                {
                                    Id = "btn_bubble",
                                    Width = buttonSize,
                                    Height = buttonSize,
                                    ImageAspect = NuiAspect.Fit,
                                    Tooltip = BubbleTooltip
                                },
                                new NuiImage("ui_dash_util")
                                {
                                    Id = "btn_utilities",
                                    Width = buttonSize,
                                    Height = buttonSize,
                                    ImageAspect = NuiAspect.Fit,
                                    Tooltip = "Open Other Utilities"
                                },
                                new NuiImage("ui_dash_tools")
                                {
                                    Id = "btn_player_tools",
                                    Width = buttonSize,
                                    Height = buttonSize,
                                    ImageAspect = NuiAspect.Fit,
                                    Tooltip = "Open Player Tools"
                                }
                            }
                        },
                        new NuiImage("ui_dash_close")
                        {
                            Id = "btn_close",
                            Width = closeButtonSize,
                            Height = closeButtonSize,
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
