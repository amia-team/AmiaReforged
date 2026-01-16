﻿using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.Classes.Spells.SpellLearning;

public sealed class SpellLearningView : ScryView<SpellLearningPresenter>
{
    // NUI Binds
    public readonly NuiBind<string> HeaderText = new("header_text");
    public readonly NuiBind<string> InstructionText = new("instruction_text");
    public readonly NuiBind<string> SpellLevelHeaderText = new("spell_level_header_text");

    // Individual spell level buttons (0-9)
    public readonly NuiBind<string> SpellLevelButtonText0 = new("spell_level_button_text_0");
    public readonly NuiBind<bool> SpellLevelButtonEnabled0 = new("spell_level_button_enabled_0");
    public readonly NuiBind<string> SpellLevelButtonText1 = new("spell_level_button_text_1");
    public readonly NuiBind<bool> SpellLevelButtonEnabled1 = new("spell_level_button_enabled_1");
    public readonly NuiBind<string> SpellLevelButtonText2 = new("spell_level_button_text_2");
    public readonly NuiBind<bool> SpellLevelButtonEnabled2 = new("spell_level_button_enabled_2");
    public readonly NuiBind<string> SpellLevelButtonText3 = new("spell_level_button_text_3");
    public readonly NuiBind<bool> SpellLevelButtonEnabled3 = new("spell_level_button_enabled_3");
    public readonly NuiBind<string> SpellLevelButtonText4 = new("spell_level_button_text_4");
    public readonly NuiBind<bool> SpellLevelButtonEnabled4 = new("spell_level_button_enabled_4");
    public readonly NuiBind<string> SpellLevelButtonText5 = new("spell_level_button_text_5");
    public readonly NuiBind<bool> SpellLevelButtonEnabled5 = new("spell_level_button_enabled_5");
    public readonly NuiBind<string> SpellLevelButtonText6 = new("spell_level_button_text_6");
    public readonly NuiBind<bool> SpellLevelButtonEnabled6 = new("spell_level_button_enabled_6");
    public readonly NuiBind<string> SpellLevelButtonText7 = new("spell_level_button_text_7");
    public readonly NuiBind<bool> SpellLevelButtonEnabled7 = new("spell_level_button_enabled_7");
    public readonly NuiBind<string> SpellLevelButtonText8 = new("spell_level_button_text_8");
    public readonly NuiBind<bool> SpellLevelButtonEnabled8 = new("spell_level_button_enabled_8");
    public readonly NuiBind<string> SpellLevelButtonText9 = new("spell_level_button_text_9");
    public readonly NuiBind<bool> SpellLevelButtonEnabled9 = new("spell_level_button_enabled_9");

    // Spell list
    public readonly NuiBind<int> SpellListCount = new("spell_list_count");
    public readonly NuiBind<bool> CanConfirm = new("can_confirm");
    public readonly NuiBind<string> SpellButtonText = new("spell_button_text");
    public readonly NuiBind<string> SpellTooltip = new("spell_tooltip");
    public readonly NuiBind<bool> SpellEnabled = new("spell_enabled");
    public readonly NuiBind<string> SpellStatusText = new("spell_status_text");
    public readonly NuiBind<string> SpellIconResRef = new("spell_icon_resref");

    public SpellLearningView(NwPlayer player, ClassType baseClass, int effectiveCasterLevel, Dictionary<int, int> spellsNeeded)
    {
        Presenter = new SpellLearningPresenter(this, player, baseClass, effectiveCasterLevel, spellsNeeded);
    }

    public override SpellLearningPresenter Presenter { get; protected set; }

    public override NuiLayout RootLayout()
    {
        // Spell list template
        List<NuiListTemplateCell> spellTemplate =
        [
            new(new NuiButton(SpellButtonText)
            {
                Id = "spell_button",
                Tooltip = SpellTooltip,
                Enabled = SpellEnabled,
                Height = 30f
            })
            {
                Width = 200f,
                VariableSize = false
            },
            new(new NuiLabel(SpellStatusText)
            {
                HorizontalAlign = NuiHAlign.Center,
                VerticalAlign = NuiVAlign.Middle
            })
            {
                Width = 120f,
                VariableSize = false
            }
        ];

        NuiRow bgLayer = new NuiRow
        {
            Width = 0f,
            Height = 0f,
            Children = new List<NuiElement>(),
            DrawList = [new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, 900f, 750f))]
        };

        NuiColumn mainColumn = new()
        {
            Children =
            {
                bgLayer,
                // Header
                new NuiRow
                {
                    Height = 40f,
                    Children =
                    {
                        new NuiLabel(HeaderText)
                        {
                            Width = 650f,
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(50, 40, 30)
                        }
                    }
                },
                // Instructions
                new NuiRow
                {
                    Height = 30f,
                    Children =
                    {
                        new NuiLabel(InstructionText)
                        {
                            Width = 650f,
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(50, 40, 30)
                        }
                    }
                },
                // Spell level selection label
                new NuiRow
                {
                    Height = 25f,
                    Children =
                    {
                        new NuiSpacer { Width = 10f },
                        new NuiLabel("Select Spell Level:")
                        {
                            Width = 640f,
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(50, 40, 30)
                        }
                    }
                },
                // Spell level buttons
                new NuiRow
                {
                    Height = 40f,
                    Children =
                    {
                        new NuiSpacer { Width = 10f },
                        new NuiButton(SpellLevelButtonText0)
                        {
                            Id = "spell_level_button_0",
                            Width = 60f,
                            Height = 35f,
                            Tooltip = "Cantrips (Level 0)",
                            Enabled = SpellLevelButtonEnabled0
                        },
                        new NuiSpacer { Width = 5f },
                        new NuiButton(SpellLevelButtonText1)
                        {
                            Id = "spell_level_button_1",
                            Width = 60f,
                            Height = 35f,
                            Tooltip = "Level 1 Spells",
                            Enabled = SpellLevelButtonEnabled1
                        },
                        new NuiSpacer { Width = 5f },
                        new NuiButton(SpellLevelButtonText2)
                        {
                            Id = "spell_level_button_2",
                            Width = 60f,
                            Height = 35f,
                            Tooltip = "Level 2 Spells",
                            Enabled = SpellLevelButtonEnabled2
                        },
                        new NuiSpacer { Width = 5f },
                        new NuiButton(SpellLevelButtonText3)
                        {
                            Id = "spell_level_button_3",
                            Width = 60f,
                            Height = 35f,
                            Tooltip = "Level 3 Spells",
                            Enabled = SpellLevelButtonEnabled3
                        },
                        new NuiSpacer { Width = 5f },
                        new NuiButton(SpellLevelButtonText4)
                        {
                            Id = "spell_level_button_4",
                            Width = 60f,
                            Height = 35f,
                            Tooltip = "Level 4 Spells",
                            Enabled = SpellLevelButtonEnabled4
                        },
                        new NuiSpacer { Width = 5f },
                        new NuiButton(SpellLevelButtonText5)
                        {
                            Id = "spell_level_button_5",
                            Width = 60f,
                            Height = 35f,
                            Tooltip = "Level 5 Spells",
                            Enabled = SpellLevelButtonEnabled5
                        },
                        new NuiSpacer { Width = 5f },
                        new NuiButton(SpellLevelButtonText6)
                        {
                            Id = "spell_level_button_6",
                            Width = 60f,
                            Height = 35f,
                            Tooltip = "Level 6 Spells",
                            Enabled = SpellLevelButtonEnabled6
                        },
                        new NuiSpacer { Width = 5f },
                        new NuiButton(SpellLevelButtonText7)
                        {
                            Id = "spell_level_button_7",
                            Width = 60f,
                            Height = 35f,
                            Tooltip = "Level 7 Spells",
                            Enabled = SpellLevelButtonEnabled7
                        },
                        new NuiSpacer { Width = 5f },
                        new NuiButton(SpellLevelButtonText8)
                        {
                            Id = "spell_level_button_8",
                            Width = 60f,
                            Height = 35f,
                            Tooltip = "Level 8 Spells",
                            Enabled = SpellLevelButtonEnabled8
                        },
                        new NuiSpacer { Width = 5f },
                        new NuiButton(SpellLevelButtonText9)
                        {
                            Id = "spell_level_button_9",
                            Width = 60f,
                            Height = 35f,
                            Tooltip = "Level 9 Spells",
                            Enabled = SpellLevelButtonEnabled9
                        }
                    }
                },
                // Current level header
                new NuiRow
                {
                    Height = 30f,
                    Children =
                    {
                        new NuiSpacer { Width = 10f },
                        new NuiLabel(SpellLevelHeaderText)
                        {
                            Width = 650f,
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(50, 40, 30)
                        }
                    }
                },
                // Scrollable spell list
                new NuiRow
                {
                    Height = 350f,
                    Width = 520f,
                    Children =
                    {
                        new NuiSpacer { Width = 145f },
                        new NuiList(spellTemplate, SpellListCount)
                        {
                            Scrollbars = NuiScrollbars.Y,
                            RowHeight = 35f
                        }
                    }
                },
                // Action buttons
                new NuiRow
                {
                    Height = 40f,
                    Children =
                    {
                        new NuiSpacer { Width = 200f },
                        new NuiButton("Confirm")
                        {
                            Id = "confirm_button",
                            Tooltip = "Confirm spell selections (must select all required spells first)",
                            Width = 100f,
                            Enabled = CanConfirm
                        },
                        new NuiSpacer { Width = 50f },
                        new NuiButton("Cancel")
                        {
                            Id = "cancel_button",
                            Tooltip = "Cancel without learning spells",
                            Width = 100f
                        }
                    }
                }
            }
        };

        return mainColumn;
    }
}
