using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Hide;

public sealed class HideEquipmentView : IScryView
{
    public NuiBind<string> Title { get; } = new("title");
    public NuiBind<string> HelmetButtonLabel { get; } = new("helmet_button_label");
    public NuiBind<string> ShieldButtonLabel { get; } = new("shield_button_label");
    public NuiBind<bool> HelmetButtonEnabled { get; } = new("helmet_button_enabled");
    public NuiBind<bool> ShieldButtonEnabled { get; } = new("shield_button_enabled");

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
                        new NuiSpacer { Width = 30f },
                        new NuiButton(HelmetButtonLabel)
                        {
                            Id = "btn_toggle_helmet",
                            Enabled = HelmetButtonEnabled,
                            Width = 120f,
                            Height = 25f
                        }
                    }
                },
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer { Width = 30f },
                        new NuiButton(ShieldButtonLabel)
                        {
                            Id = "btn_toggle_shield",
                            Enabled = ShieldButtonEnabled,
                            Width = 120f,
                            Height = 25f
                        }
                    }
                }
            }
        };
        return root;
    }
}
