using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Utilities;

public sealed class UtilitiesView : IScryView
{
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
                        new NuiImage("ui_util_save")
                        {
                            Id = "btn_save",
                            Tooltip = "Save Character",
                            Width = 40f,
                            Height = 40f,
                            ImageAspect = NuiAspect.Fit
                        },
                        new NuiImage("ui_util_summon")
                        {
                            Id = "btn_summon",
                            Tooltip = "Summon Options",
                            Width = 40f,
                            Height = 40f,
                            ImageAspect = NuiAspect.Fit
                        },
                        new NuiImage("ui_util_self")
                        {
                            Id = "btn_self",
                            Tooltip = "Self Settings",
                            Width = 40f,
                            Height = 40f,
                            ImageAspect = NuiAspect.Fit
                        },
                        new NuiImage("ui_util_game")
                        {
                            Id = "btn_game",
                            Tooltip = "Game Settings",
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
