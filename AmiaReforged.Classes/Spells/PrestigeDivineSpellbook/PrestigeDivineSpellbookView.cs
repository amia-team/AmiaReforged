using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.Classes.Spells.PrestigeDivineSpellbook;

/// <summary>
/// NUI window for prestige-boosted divine casters (Rangers/Paladins) to memorize and manage
/// spells they have access to but the character sheet UI doesn't display due to level restrictions.
/// </summary>
public sealed class PrestigeDivineSpellbookView : ScryView<PrestigeDivineSpellbookPresenter>
{
    private const int MaxSpellsPerLevel = 20;  // Maximum spells to display per level

    // Header and instructions
    public readonly NuiBind<string> HeaderText = new("header_text");
    public readonly NuiBind<string> InstructionText = new("instruction_text");
    public readonly NuiBind<string> ClassNameText = new("class_name_text");
    public readonly NuiBind<string> CasterLevelText = new("caster_level_text");

    // Spell level buttons (1-4 for Rangers/Paladins)
    public readonly NuiBind<string> SpellLevelButtonText1 = new("spell_level_button_text_1");
    public readonly NuiBind<bool> SpellLevelButtonEnabled1 = new("spell_level_button_enabled_1");

    public readonly NuiBind<string> SpellLevelButtonText2 = new("spell_level_button_text_2");
    public readonly NuiBind<bool> SpellLevelButtonEnabled2 = new("spell_level_button_enabled_2");

    public readonly NuiBind<string> SpellLevelButtonText3 = new("spell_level_button_text_3");
    public readonly NuiBind<bool> SpellLevelButtonEnabled3 = new("spell_level_button_enabled_3");

    public readonly NuiBind<string> SpellLevelButtonText4 = new("spell_level_button_text_4");
    public readonly NuiBind<bool> SpellLevelButtonEnabled4 = new("spell_level_button_enabled_4");

    // Spell list display - array binds for each spell
    public readonly NuiBind<int> SpellListCount = new("spell_list_count");
    public readonly List<NuiBind<string>> SpellNames = [];
    public readonly List<NuiBind<string>> SpellStatus = [];  // "✓" or ""
    public readonly List<NuiBind<string>> SpellButtonColor = [];
    public readonly List<NuiBind<bool>> SpellVisible = [];

    // Slots info display
    public readonly NuiBind<string> SlotsInfoText = new("slots_info_text");

    public PrestigeDivineSpellbookView(NwPlayer player, ClassType classType, NwCreature creature)
    {
        // Initialize the array binds
        for (int i = 0; i < MaxSpellsPerLevel; i++)
        {
            SpellNames.Add(new NuiBind<string>($"spell_name_{i}"));
            SpellStatus.Add(new NuiBind<string>($"spell_status_{i}"));
            SpellButtonColor.Add(new NuiBind<string>($"spell_color_{i}"));
            SpellVisible.Add(new NuiBind<bool>($"spell_visible_{i}"));
        }

        Presenter = new PrestigeDivineSpellbookPresenter(this, player, classType, creature);
    }

    public override PrestigeDivineSpellbookPresenter Presenter { get; protected set; }

    public override NuiLayout RootLayout()
    {
        NuiColumn mainColumn = new()
        {
            Children = new List<NuiElement>
            {
                new NuiLabel(HeaderText)
                {
                    Height = 25f,
                    HorizontalAlign = NuiHAlign.Center,
                    ForegroundColor = new Color(50, 150, 200)
                },
                new NuiSpacer { Height = 5f },
                new NuiRow
                {
                    Height = 40f,
                    Children = new List<NuiElement>
                    {
                        new NuiColumn
                        {
                            Children = new List<NuiElement>
                            {
                                new NuiLabel(ClassNameText) { Height = 20f },
                                new NuiLabel(CasterLevelText) { Height = 20f }
                            }
                        }
                    }
                },
                new NuiSpacer { Height = 5f },
                new NuiLabel(InstructionText)
                {
                    Height = 35f
                },
                new NuiRow
                {
                    Height = 50f,
                    Children = new List<NuiElement>
                    {
                        new NuiButton(SpellLevelButtonText1)
                        {
                            Id = "spell_level_button_1",
                            Enabled = SpellLevelButtonEnabled1,
                            Width = 60f
                        },
                        new NuiSpacer { Width = 5f },
                        new NuiButton(SpellLevelButtonText2)
                        {
                            Id = "spell_level_button_2",
                            Enabled = SpellLevelButtonEnabled2,
                            Width = 60f
                        },
                        new NuiSpacer { Width = 5f },
                        new NuiButton(SpellLevelButtonText3)
                        {
                            Id = "spell_level_button_3",
                            Enabled = SpellLevelButtonEnabled3,
                            Width = 60f
                        },
                        new NuiSpacer { Width = 5f },
                        new NuiButton(SpellLevelButtonText4)
                        {
                            Id = "spell_level_button_4",
                            Enabled = SpellLevelButtonEnabled4,
                            Width = 60f
                        },
                        new NuiSpacer()
                    }
                },
                new NuiLabel(SlotsInfoText)
                {
                    Height = 20f,
                    HorizontalAlign = NuiHAlign.Center
                },
                new NuiColumn
                {
                    Children = BuildSpellListElements()
                },
                new NuiRow
                {
                    Height = 30f,
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer(),
                        new NuiButton("Confirm")
                        {
                            Id = "confirm_button",
                            Width = 80f
                        },
                        new NuiSpacer()
                    }
                }
            }
        };

        return mainColumn;
    }

    private List<NuiElement> BuildSpellListElements()
    {
        List<NuiElement> elements = new();

        for (int i = 0; i < MaxSpellsPerLevel; i++)
        {
            elements.Add(new NuiRow
            {
                Height = 30f,
                Children = new List<NuiElement>
                {
                    new NuiButton(SpellNames[i])
                    {
                        Id = $"spell_button_{i}",
                        Width = 275f,
                        ForegroundColor = ColorConstants.White
                    },
                    new NuiLabel(SpellStatus[i])
                    {
                        HorizontalAlign = NuiHAlign.Center,
                        VerticalAlign = NuiVAlign.Middle,
                        Width = 50f
                    },
                    new NuiSpacer { Width = 10f }
                },
                Visible = SpellVisible[i]
            });
        }

        return elements;
    }
}





