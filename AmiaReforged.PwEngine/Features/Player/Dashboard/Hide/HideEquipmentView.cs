using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Hide;

public sealed class HideEquipmentView : IScryView
{
    public NuiBind<string> Title { get; } = new("title");
    public NuiBind<string> HelmetTooltip { get; } = new("helmet_tooltip");
    public NuiBind<string> CloakTooltip { get; } = new("cloak_tooltip");
    public NuiBind<string> ShieldTooltip { get; } = new("shield_tooltip");

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
                        new NuiImage("ui_dash_helm")
                        {
                            Id = "btn_toggle_helmet",
                            Tooltip = HelmetTooltip,
                            Width = 40f,
                            Height = 40f,
                            ImageAspect = NuiAspect.Fit
                        },
                        new NuiImage("ui_dash_shield")
                        {
                            Id = "btn_toggle_shield",
                            Tooltip = ShieldTooltip,
                            Width = 40f,
                            Height = 40f,
                            ImageAspect = NuiAspect.Fit
                        },
                        new NuiImage("ui_dash_cloak")
                        {
                            Id = "btn_toggle_cloak",
                            Tooltip = CloakTooltip,
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
