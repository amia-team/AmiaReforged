using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Utilities.SummonOptions;

public sealed class SummonOptionsView : IScryView
{
    // Binds for dropdowns
    public NuiBind<int> EdkSelection { get; } = new("edk_selection");
    public NuiBind<int> EmdSelection { get; } = new("emd_selection");
    public NuiBind<int> SummonCreatureSelection { get; } = new("sc_selection");
    public NuiBind<int> BlackguardSelection { get; } = new("bg_selection");

    // Labels that show current selection
    public NuiBind<string> EdkCurrentLabel { get; } = new("edk_current");
    public NuiBind<string> EmdCurrentLabel { get; } = new("emd_current");
    public NuiBind<string> ScCurrentLabel { get; } = new("sc_current");
    public NuiBind<string> BgCurrentLabel { get; } = new("bg_current");

    // Enabled binds for each section
    public NuiBind<bool> EdkEnabled { get; } = new("edk_enabled");
    public NuiBind<bool> EmdEnabled { get; } = new("emd_enabled");
    public NuiBind<bool> BgEnabled { get; } = new("bg_enabled");

    // Epic Dragon Knight options (from edk_select.nss)
    private static readonly List<NuiComboEntry> EdkOptions = new()
    {
        new NuiComboEntry("Brass", 1),
        new NuiComboEntry("Bronze", 2),
        new NuiComboEntry("Copper", 3),
        new NuiComboEntry("Gold", 4),
        new NuiComboEntry("Silver", 5),
        new NuiComboEntry("Black", 6),
        new NuiComboEntry("Blue", 7),
        new NuiComboEntry("Green", 8),
        new NuiComboEntry("Red", 9),
        new NuiComboEntry("White", 10),
        new NuiComboEntry("Behir", 11),
        new NuiComboEntry("Earth Drake", 12),
        new NuiComboEntry("Prismatic", 13),
        new NuiComboEntry("Shadow", 14),
        new NuiComboEntry("Undead", 15)
    };

    // Epic Mummy Dust options (typical summon types)
    private static readonly List<NuiComboEntry> EmdOptions = new()
    {
        new NuiComboEntry("Undead", 1),
        new NuiComboEntry("Celestial", 2),
        new NuiComboEntry("Construct", 3),
        new NuiComboEntry("Magical Beast", 4),
        new NuiComboEntry("Elemental", 5)
    };

    // Summon Creature spell options
    private static readonly List<NuiComboEntry> SummonCreatureOptions = new()
    {
        new NuiComboEntry("Animal", 0),
        new NuiComboEntry("Elemental", 1),
        new NuiComboEntry("Vermin", 2),
        new NuiComboEntry("Celestial", 3),
        new NuiComboEntry("Fiendish", 4),
        new NuiComboEntry("Wild", 5)
    };

    // Blackguard summon options (from bg_choice_*.nss: CE=1, LE=2, NE=3)
    private static readonly List<NuiComboEntry> BlackguardOptions = new()
    {
        new NuiComboEntry("Kelvezu (CE)", 1),
        new NuiComboEntry("Amnizu (LE)", 2),
        new NuiComboEntry("Ultroloth (NE)", 3)
    };

    public NuiLayout RootLayout()
    {
        Color labelColor = new Color(30, 20, 12);

        NuiColumn root = new()
        {
            Children = new List<NuiElement>
            {
                // Background image in a zero-sized row
                new NuiRow
                {
                    Width = 0f,
                    Height = 0f,
                    Children = new List<NuiElement>(),
                    DrawList = new List<NuiDrawListItem>
                    {
                        new NuiDrawListImage("ui_bg", new NuiRect(0, 0, 320f, 350f))
                    }
                },
                new NuiSpacer { Height = 10f },

                // Summon Creature section (available to everyone)
                new NuiRow
                {
                    Height = 25f,
                    Children = new List<NuiElement>
                    {
                        new NuiLabel("Summon Creature:")
                        {
                            Width = 150f,
                            ForegroundColor = labelColor,
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle
                        },
                        new NuiLabel(ScCurrentLabel)
                        {
                            Width = 100f,
                            ForegroundColor = new Color(60, 30, 30),
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle
                        }
                    }
                },
                new NuiRow
                {
                    Height = 35f,
                    Children = new List<NuiElement>
                    {
                        new NuiCombo
                        {
                            Id = "combo_sc",
                            Width = 200f,
                            Height = 30f,
                            Entries = SummonCreatureOptions,
                            Selected = SummonCreatureSelection
                        },
                        new NuiButton("Set")
                        {
                            Id = "btn_set_sc",
                            Width = 60f,
                            Height = 30f
                        }
                    }
                },
                new NuiSpacer { Height = 5f },

                // Blackguard section (requires 5+ Blackguard levels)
                new NuiRow
                {
                    Height = 25f,
                    Children = new List<NuiElement>
                    {
                        new NuiLabel("Blackguard Fiend:")
                        {
                            Width = 150f,
                            ForegroundColor = labelColor,
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle
                        },
                        new NuiLabel(BgCurrentLabel)
                        {
                            Width = 100f,
                            ForegroundColor = new Color(60, 30, 30),
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle
                        }
                    }
                },
                new NuiRow
                {
                    Height = 35f,
                    Children = new List<NuiElement>
                    {
                        new NuiCombo
                        {
                            Id = "combo_bg",
                            Width = 200f,
                            Height = 30f,
                            Entries = BlackguardOptions,
                            Selected = BlackguardSelection,
                            Enabled = BgEnabled
                        },
                        new NuiButton("Set")
                        {
                            Id = "btn_set_bg",
                            Width = 60f,
                            Height = 30f,
                            Enabled = BgEnabled
                        }
                    }
                },
                new NuiSpacer { Height = 5f },

                // Epic Mummy Dust section (requires feat 874)
                new NuiRow
                {
                    Height = 25f,
                    Children = new List<NuiElement>
                    {
                        new NuiLabel("Epic Mummy Dust:")
                        {
                            Width = 150f,
                            ForegroundColor = labelColor,
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle
                        },
                        new NuiLabel(EmdCurrentLabel)
                        {
                            Width = 100f,
                            ForegroundColor = new Color(60, 30, 30),
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle
                        }
                    }
                },
                new NuiRow
                {
                    Height = 35f,
                    Children = new List<NuiElement>
                    {
                        new NuiCombo
                        {
                            Id = "combo_emd",
                            Width = 200f,
                            Height = 30f,
                            Entries = EmdOptions,
                            Selected = EmdSelection,
                            Enabled = EmdEnabled
                        },
                        new NuiButton("Set")
                        {
                            Id = "btn_set_emd",
                            Width = 60f,
                            Height = 30f,
                            Enabled = EmdEnabled
                        }
                    }
                },
                new NuiSpacer { Height = 5f },

                // Epic Dragon Knight section (requires feat 875)
                new NuiRow
                {
                    Height = 25f,
                    Children = new List<NuiElement>
                    {
                        new NuiLabel("Epic Dragon Knight:")
                        {
                            Width = 150f,
                            ForegroundColor = labelColor,
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle
                        },
                        new NuiLabel(EdkCurrentLabel)
                        {
                            Width = 100f,
                            ForegroundColor = new Color(60, 30, 30),
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle
                        }
                    }
                },
                new NuiRow
                {
                    Height = 35f,
                    Children = new List<NuiElement>
                    {
                        new NuiCombo
                        {
                            Id = "combo_edk",
                            Width = 200f,
                            Height = 30f,
                            Entries = EdkOptions,
                            Selected = EdkSelection,
                            Enabled = EdkEnabled
                        },
                        new NuiButton("Set")
                        {
                            Id = "btn_set_edk",
                            Width = 60f,
                            Height = 30f,
                            Enabled = EdkEnabled
                        }
                    }
                }
            }
        };
        return root;
    }

    // Helper methods to get labels from selection values
    public static string GetEdkLabel(int selection)
    {
        return selection switch
        {
            1 => "Brass",
            2 => "Bronze",
            3 => "Copper",
            4 => "Gold",
            5 => "Silver",
            6 => "Black",
            7 => "Blue",
            8 => "Green",
            9 => "Red",
            10 => "White",
            11 => "Behir",
            12 => "Earth Drake",
            13 => "Prismatic",
            14 => "Shadow",
            15 => "Undead",
            _ => "Not Set"
        };
    }

    public static string GetEmdLabel(int selection)
    {
        return selection switch
        {
            1 => "Undead",
            2 => "Celestial",
            3 => "Construct",
            4 => "Magical Beast",
            5 => "Elemental",
            _ => "Not Set"
        };
    }

    public static string GetScLabel(int selection)
    {
        return selection switch
        {
            0 => "Animal",
            1 => "Elemental",
            2 => "Vermin",
            3 => "Celestial",
            4 => "Fiendish",
            5 => "Wild",
            _ => "Not Set"
        };
    }

    public static string GetBgLabel(int selection)
    {
        return selection switch
        {
            1 => "Chaotic Evil",
            2 => "Lawful Evil",
            3 => "Neutral Evil",
            _ => "Not Set"
        };
    }
}
