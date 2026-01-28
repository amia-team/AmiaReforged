using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Utilities.SelfSettings;

public sealed class HurtYourselfView : IScryView
{
    public NuiBind<int> DamageSelected { get; } = new("damage_selected");
    public NuiBind<List<NuiComboEntry>> DamageOptions { get; } = new("damage_options");

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
                    DrawList = new List<NuiDrawListItem> { new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, 320f, 250f)) }
                },
                new NuiSpacer { Height = 10f },
                new NuiLabel("Select Damage Amount:")
                {
                    Height = 20f,
                    ForegroundColor = new Color(30, 20, 12),
                    HorizontalAlign = NuiHAlign.Center
                },
                new NuiSpacer { Height = 10f },
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer { Width = 10f },
                        new NuiCombo
                        {
                            Entries = DamageOptions,
                            Selected = DamageSelected,
                            Width = 260f,
                            Height = 25f
                        },
                        new NuiSpacer { Width = 10f }
                    }
                },
                new NuiSpacer { Height = 15f },
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer(),
                        new NuiButton("Apply Damage")
                        {
                            Id = "btn_apply",
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
