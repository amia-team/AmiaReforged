using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Utilities.GameSettings;

public sealed class PvpToolView : IScryView
{
    public NuiBind<string> TargetName { get; } = new("target_name");
    public NuiBind<string> TargetStatus { get; } = new("target_status");
    public NuiBind<bool> ToggleButtonEnabled { get; } = new("toggle_enabled");
    public NuiBind<string> ToggleButtonLabel { get; } = new("toggle_label");

    // Raise dead PvP victim bindings
    public NuiBind<bool> RaiseButtonEnabled { get; } = new("raise_enabled");

    // PvP Mode bindings
    public NuiBind<string> CurrentModeLabel { get; } = new("current_mode");

    public NuiLayout RootLayout()
    {
        Color labelColor = new Color(30, 20, 12);

        NuiColumn root = new()
        {
            Width = 300f,
            Children = new List<NuiElement>
            {
                new NuiRow
                {
                    Width = 0f,
                    Height = 0f,
                    Children = new List<NuiElement>(),
                    DrawList = new List<NuiDrawListItem> { new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, 330f, 460f)) }
                },
                new NuiSpacer { Height = 5f },
                new NuiLabel("PvP Settings (WIP)")
                {
                    Height = 20f,
                    Width = 290f,
                    HorizontalAlign = NuiHAlign.Center,
                    ForegroundColor = new Color(30, 20, 12)
                },
                new NuiSpacer { Height = 5f },

                // PvP Mode Section
                new NuiLabel("Your PvP Mode:")
                {
                    Height = 20f,
                    Width = 290f,
                    HorizontalAlign = NuiHAlign.Center,
                    ForegroundColor = labelColor
                },
                new NuiLabel(CurrentModeLabel)
                {
                    Height = 25f,
                    Width = 290f,
                    HorizontalAlign = NuiHAlign.Center,
                    ForegroundColor = new Color(60, 30, 30)
                },
                new NuiSpacer { Height = 5f },
                new NuiRow
                {
                    Width = 290f,
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer{ Width = 15f},
                        new NuiButton("Subdual")
                        {
                            Id = "btn_mode_subdual",
                            Width = 80f,
                            Height = 30f,
                            Tooltip = "Default mode - enemies are knocked unconscious"
                        },
                        new NuiSpacer { Width = 5f },
                        new NuiButton("Duel")
                        {
                            Id = "btn_mode_duel",
                            Width = 80f,
                            Height = 30f,
                            Tooltip = "Honor duel - can be raised after death"
                        },
                        new NuiSpacer { Width = 5f },
                        new NuiButton("Brawl")
                        {
                            Id = "btn_mode_brawl",
                            Width = 80f,
                            Height = 30f,
                            Tooltip = "Serious fight - full PvP consequences"
                        }
                    }
                },
                new NuiSpacer { Height = 10f },

                // Target Section
                new NuiLabel("Target a player to like/dislike them,")
                {
                    Height = 20f,
                    Width = 290f,
                    HorizontalAlign = NuiHAlign.Center,
                    ForegroundColor = labelColor
                },
                new NuiLabel("or target a PvP corpse to raise them.")
                {
                    Height = 20f,
                    Width = 290f,
                    HorizontalAlign = NuiHAlign.Center,
                    ForegroundColor = labelColor
                },
                new NuiRow
                {
                    Width = 290f,
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer { Width = 50f },
                        new NuiButtonImage("nui_pick")
                        {
                            Id = "btn_select_target",
                            Width = 32f,
                            Height = 32f,
                            Tooltip = "Select Target"
                        },
                        new NuiSpacer { Width = 10f },
                        new NuiLabel(TargetName)
                        {
                            Width = 190f,
                            Height = 32f,
                            ForegroundColor = labelColor,
                            VerticalAlign = NuiVAlign.Middle
                        }
                    }
                },
                new NuiLabel(TargetStatus)
                {
                    Height = 20f,
                    Width = 290f,
                    HorizontalAlign = NuiHAlign.Center,
                    ForegroundColor = new Color(60, 30, 30)
                },

                // Like/Dislike button row
                new NuiRow
                {
                    Width = 290f,
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer { Width = 50f },
                        new NuiButton(ToggleButtonLabel)
                        {
                            Id = "btn_toggle",
                            Enabled = ToggleButtonEnabled,
                            Width = 180f,
                            Height = 30f
                        }
                    }
                },
                // Raise PvP victim button row
                new NuiRow
                {
                    Width = 290f,
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer{ Width = 50f },
                        new NuiButton("Raise PvP Victim")
                        {
                            Id = "btn_raise",
                            Enabled = RaiseButtonEnabled,
                            Width = 180f,
                            Height = 30f,
                            Tooltip = "Target a PvP corpse to raise them"
                        }
                    }
                },
                new NuiSpacer { Height = 10f },
                new NuiRow
                {
                    Width = 290f,
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer{ Width = 80f},
                        new NuiButton("Close")
                        {
                            Id = "btn_close",
                            Width = 120f,
                            Height = 30f
                        }
                    }
                },
                new NuiSpacer { Height = 20f }
            }
        };
        return root;
    }
}
