using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Hide;

public sealed class HideEquipmentView : IScryView
{
    // Scale factor for GUI scaling compensation (1.0 = no scaling)
    private float _scaleFactor = 1.0f;

    // Base sizes for elements (at 100% GUI scale)
    private const float BaseButtonSize = 40f;

    public NuiBind<string> Title { get; } = new("title");
    public NuiBind<string> HelmetTooltip { get; } = new("helmet_tooltip");
    public NuiBind<string> CloakTooltip { get; } = new("cloak_tooltip");
    public NuiBind<string> ShieldTooltip { get; } = new("shield_tooltip");

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
                        new NuiImage("ui_dash_helm")
                        {
                            Id = "btn_toggle_helmet",
                            Tooltip = HelmetTooltip,
                            Width = buttonSize,
                            Height = buttonSize,
                            ImageAspect = NuiAspect.Fit
                        },
                        new NuiImage("ui_dash_shield")
                        {
                            Id = "btn_toggle_shield",
                            Tooltip = ShieldTooltip,
                            Width = buttonSize,
                            Height = buttonSize,
                            ImageAspect = NuiAspect.Fit
                        },
                        new NuiImage("ui_dash_cloak")
                        {
                            Id = "btn_toggle_cloak",
                            Tooltip = CloakTooltip,
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
