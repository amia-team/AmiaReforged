using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.CharacterTools.CustomSummon;

public sealed class WeaponChangeView : ScryView<WeaponChangePresenter>
{
    private const float WindowW = 430f;
    private const float WindowH = 500f;

    public override WeaponChangePresenter Presenter { get; protected set; } = null!;

    // General control binds
    public readonly NuiBind<bool> AlwaysEnabled = new("wc_always_enabled");

    // List binds
    public readonly NuiBind<int> WeaponTypeCount = new("wc_weapon_count");
    public readonly NuiBind<string> WeaponTypeNames = new("wc_weapon_names");
    public readonly NuiBind<int> SelectedWeaponIndex = new("wc_selected_weapon");

    public WeaponChangeView(NwPlayer player, NwCreature targetCreature, NwItem widget)
    {
        Presenter = new WeaponChangePresenter(this, player, targetCreature, widget);
    }

    public override NuiLayout RootLayout()
    {
        NuiRow bgLayer = new NuiRow
        {
            Width = 0f,
            Height = 0f,
            Children = new List<NuiElement>(),
            DrawList = [new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, WindowW, WindowH))]
        };

        return new NuiColumn
        {
            Width = WindowW,
            Height = WindowH,
            Children =
            {
                bgLayer,
                BuildMainContent()
            }
        };
    }

    private NuiElement BuildMainContent()
    {
        List<NuiListTemplateCell> weaponRowTemplate =
        [
            new(new NuiButton(WeaponTypeNames)
            {
                Id = "wc_btn_select_weapon",
                Width = 320f,
                Height = 35f
            })
        ];

        return new NuiColumn
        {
            Children =
            {
                new NuiSpacer { Height = 15f },
                // Title
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 100f },
                        new NuiLabel("Select Weapon Type")
                        {
                            Height = 25f,
                            Width = 200f,
                            HorizontalAlign = NuiHAlign.Center,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },
                new NuiSpacer { Height = 10f },

                // Instructions
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 70f },
                        new NuiLabel("Choose a compatible weapon type:")
                        {
                            Height = 40f,
                            Width = 320f,
                            ForegroundColor = new Color(50, 40, 30)
                        }
                    }
                },
                new NuiSpacer { Height = 10f },

                // Weapon type list
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 40f },
                        new NuiList(weaponRowTemplate, WeaponTypeCount)
                        {
                            RowHeight = 40f,
                            Height = 240f,
                            Width = 320f
                        }
                    }
                },
                new NuiSpacer { Height = 15f },

                // Action buttons
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 90f },
                        new NuiButton("Confirm")
                        {
                            Id = "wc_btn_confirm",
                            Width = 100f,
                            Height = 35f,
                            Tooltip = "Change weapon to selected type"
                        },
                        new NuiSpacer { Width = 10f },
                        new NuiButton("Cancel")
                        {
                            Id = "wc_btn_cancel",
                            Width = 100f,
                            Height = 35f,
                            Tooltip = "Close without changing"
                        }
                    }
                }
            }
        };
    }
}

