using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Utilities.GameSettings;

public sealed class EmoteSymbolView : IScryView
{
    public NuiBind<string> SymbolInput { get; } = new("symbol_input");

    public NuiLayout RootLayout()
    {
        NuiColumn root = new()
        {
            Children = new List<NuiElement>
            {
                new NuiRow
                {
                    Width = 0f,
                    Height = 0f,
                    Children = new List<NuiElement>(),
                    DrawList = new List<NuiDrawListItem> { new NuiDrawListImage("ui_bg", new NuiRect(-10f, -10f, 390f, 290f)) }
                },
                new NuiSpacer { Height = 10f },
                new NuiLabel("Set Emote Symbol:")
                {
                    Height = 20f,
                    ForegroundColor = new Color(30, 20, 12),
                    HorizontalAlign = NuiHAlign.Center
                },
                new NuiSpacer { Height = 5f },
                new NuiLabel("Enter a single character to denote emotes.")
                {
                    Height = 35f,
                    HorizontalAlign = NuiHAlign.Center,
                    ForegroundColor = new Color(30, 20, 12)
                },
                new NuiSpacer { Height = 10f },
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer { Width = 10f },
                        new NuiTextEdit("Symbol", SymbolInput, 1, false)
                        {
                            Width = 300f,
                            Height = 25f
                        },
                        new NuiSpacer { Width = 10f }
                    }
                },
                new NuiSpacer { Height = 5f },
                new NuiLabel("(Double-quotes are supported.)")
                {
                    Height = 20f,
                    HorizontalAlign = NuiHAlign.Center,
                    ForegroundColor = new Color(30, 20, 12)
                },
                new NuiSpacer { Height = 15f },
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer(),
                        new NuiButton("Set Symbol")
                        {
                            Id = "btn_set",
                            Width = 120f,
                            Height = 35f
                        },
                        new NuiSpacer { Width = 10f },
                        new NuiButton("Cancel")
                        {
                            Id = "btn_cancel",
                            Width = 100f,
                            Height = 35f
                        },
                        new NuiSpacer()
                    }
                },
                new NuiSpacer { Height = 10f }
            }
        };
        return root;
    }
}
