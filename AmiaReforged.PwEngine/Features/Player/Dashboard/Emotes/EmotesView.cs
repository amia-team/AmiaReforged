using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Emotes;

public sealed class EmotesView : IScryView
{
    // Binds
    public NuiBind<string> TargetName { get; } = new("target_name");
    public NuiBind<List<NuiComboEntry>> IndividualEmoteOptions { get; } = new("individual_emotes");
    public NuiBind<List<NuiComboEntry>> MutualEmoteOptions { get; } = new("mutual_emotes");
    public NuiBind<int> SelectedIndividual { get; } = new("selected_individual");
    public NuiBind<int> SelectedMutual { get; } = new("selected_mutual");
    public NuiBind<bool> PerformButtonEnabled { get; } = new("perform_button_enabled");

    public NuiLayout RootLayout()
    {
        // Background layer - DrawList doesn't participate in constraints
        NuiRow bgLayer = new()
        {
            Width = 0f,
            Height = 0f,
            Children = new List<NuiElement>(),
            DrawList = new List<NuiDrawListItem> { new NuiDrawListImage("ui_bg_emote", new NuiRect(0f, 0f, 300, 300)) }
        };

        // Simple flat column structure - no nesting
        NuiColumn root = new()
        {
            Children = new List<NuiElement>
            {
                bgLayer,
                new NuiRow { Children =
                    {
                        new NuiSpacer { Width = 27f },
                        new NuiLabel("Select Self (Default), an Associate,")
                        {
                            Height = 20f,
                            Width = 290f
                        }
                    }
                },
                new NuiRow { Children =
                    {
                        new NuiSpacer { Width = 25f },
                        new NuiLabel("or another Player for Mutual Emotes.")
                        {
                            Height = 20f,
                            Width = 290f
                        }
                    }
                },

                // Target selection
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer { Width = 25f },
                        new NuiButtonImage("nui_pick")
                        {
                            Id = "btn_pick_target",
                            Width = 35f,
                            Height = 35f,
                            Tooltip = "Select Target"
                        },
                        new NuiLabel("Target:")
                        {
                            Width = 50f,
                            Height = 35f,
                            VerticalAlign = NuiVAlign.Middle
                        },
                        new NuiLabel(TargetName)
                        {
                            Width = 180f,
                            Height = 35f,
                            VerticalAlign = NuiVAlign.Middle
                        }
                    }
                },

                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 50f },
                        // Individual emotes label
                        new NuiLabel("Individual Emotes")
                        {
                            Height = 20f,
                            Width = 150f
                        }
                    }
                },

                new NuiRow
                {
                    Children =
                    {
                        // Individual emotes combo
                        new NuiSpacer { Width = 50f },
                        new NuiCombo
                        {
                            Id = "combo_individual",
                            Width = 150f,
                            Height = 25f,
                            Entries = IndividualEmoteOptions,
                            Selected = SelectedIndividual
                        },

                        // Individual emotes button
                        new NuiButton("Go!")
                        {
                            Id = "btn_perform_individual",
                            Tooltip = "Perform Individual Emote",
                            Width = 45f,
                            Height = 25f
                        }
                    }
                },

                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 50f },
                        // Mutual emotes label
                        new NuiLabel("Mutual Emotes")
                        {
                            Height = 20f,
                            Width = 150f
                        }
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        // Mutual emotes combo
                        new NuiSpacer { Width = 50f },
                        new NuiCombo
                        {
                            Id = "combo_mutual",
                            Width = 150f,
                            Height = 25f,
                            Entries = MutualEmoteOptions,
                            Selected = SelectedMutual
                        },
                        // Mutual emotes button
                        new NuiButton("Go!")
                        {
                            Id = "btn_perform_mutual",
                            Width = 45f,
                            Height = 25f,
                            Enabled = PerformButtonEnabled
                        }
                    }
                },
                new NuiSpacer{ Height = 50f}
            }
        };
        return root;
    }
}
