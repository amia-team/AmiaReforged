using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Utilities.SelfSettings;

public sealed class SelfSettingsView : IScryView
{
    public NuiBind<string> BubbleTooltip { get; } = new("bubble_tooltip");

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
                        new NuiImage("ui_util_bubble")
                        {
                            Id = "btn_bubble",
                            Tooltip = BubbleTooltip,
                            Width = 40f,
                            Height = 40f,
                            ImageAspect = NuiAspect.Fit
                        },
                        new NuiImage("ui_util_ouch")
                        {
                            Id = "btn_hurt",
                            Tooltip = "Hurt Yourself",
                            Width = 40f,
                            Height = 40f,
                            ImageAspect = NuiAspect.Fit
                        },
                        new NuiImage("ui_util_acp")
                        {
                            Id = "btn_acp",
                            Tooltip = "Animation Combat Phenotype",
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
