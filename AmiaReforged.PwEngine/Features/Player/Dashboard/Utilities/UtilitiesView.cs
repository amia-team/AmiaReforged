using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Utilities;

public sealed class UtilitiesView : IScryView
{
    // Scale factor for GUI scaling compensation (1.0 = no scaling)
    private float _scaleFactor = 1.0f;

    // Base sizes for elements (at 100% GUI scale)
    private const float BaseButtonSize = 40f;

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
                        new NuiImage("ui_util_save")
                        {
                            Id = "btn_save",
                            Tooltip = "Save Character",
                            Width = buttonSize,
                            Height = buttonSize,
                            ImageAspect = NuiAspect.Fit
                        },
                        new NuiImage("ui_util_summon")
                        {
                            Id = "btn_summon",
                            Tooltip = "Summon Options",
                            Width = buttonSize,
                            Height = buttonSize,
                            ImageAspect = NuiAspect.Fit
                        },
                        new NuiImage("ui_util_self")
                        {
                            Id = "btn_self",
                            Tooltip = "Self Settings",
                            Width = buttonSize,
                            Height = buttonSize,
                            ImageAspect = NuiAspect.Fit
                        },
                        new NuiImage("ui_util_game")
                        {
                            Id = "btn_game",
                            Tooltip = "Game Settings",
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
