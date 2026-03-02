using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Utilities.SelfSettings;

public sealed class SelfSettingsView : IScryView
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
                        new NuiImage("ui_util_ouch")
                        {
                            Id = "btn_hurt",
                            Tooltip = "Hurt Yourself",
                            Width = buttonSize,
                            Height = buttonSize,
                            ImageAspect = NuiAspect.Fit
                        },
                        new NuiImage("ui_util_acp")
                        {
                            Id = "btn_acp",
                            Tooltip = "Phenotype Changer",
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
