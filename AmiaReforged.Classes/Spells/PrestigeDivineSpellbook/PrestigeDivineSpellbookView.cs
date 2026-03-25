using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.Classes.Spells.PrestigeDivineSpellbook;

/// <summary>
/// NUI window for prestige-boosted divine casters (Rangers/Paladins) to memorize and manage
/// spells they have access to but the character sheet UI doesn't display due to level restrictions.
/// </summary>
public sealed class PrestigeDivineSpellbookView : ScryView<PrestigeDivineSpellbookPresenter>
{
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

    // Spell list display
    public readonly NuiBind<int> SpellListCount = new("spell_list_count");
    public readonly NuiBind<string> SpellButtonText = new("spell_button_text");
    public readonly NuiBind<string> SpellStatusText = new("spell_status_text");
    public readonly NuiBind<string> SpellTooltip = new("spell_tooltip");
    public readonly NuiBind<Color> SpellButtonColor = new("spell_button_color");
    public readonly NuiBind<bool> SpellEnabled = new("spell_enabled");

    // Slots info display
    public readonly NuiBind<string> SlotsInfoText = new("slots_info_text");

    public PrestigeDivineSpellbookView(NwPlayer player, ClassType classType, NwCreature creature)
    {
        Presenter = new PrestigeDivineSpellbookPresenter(this, player, classType, creature);
    }

    public override PrestigeDivineSpellbookPresenter Presenter { get; protected set; }

    public override NuiLayout RootLayout()
    {
        // Spell list template
        List<NuiListTemplateCell> spellTemplate = new()
        {
            new(new NuiButton(SpellButtonText)
            {
                Id = "spell_button",
                Tooltip = SpellTooltip,
                Enabled = SpellEnabled,
                Height = 40f,
                ForegroundColor = SpellButtonColor
            })
            {
                Width = 275f,
                VariableSize = false
            },
            new(new NuiLabel(SpellStatusText)
            {
                HorizontalAlign = NuiHAlign.Center,
                VerticalAlign = NuiVAlign.Middle
            })
            {
                Width = 110f,
                VariableSize = false
            }
        };

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
                new NuiList(spellTemplate, SpellListCount)
                {
                    Height = 300f,
                    RowHeight = 40f
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
}





