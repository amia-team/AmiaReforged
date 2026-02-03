using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Pray;

public sealed class DeitySelectionView : IScryView
{
    // Deity info bindings
    public NuiBind<string> DeityName { get; } = new("deity_name");
    public NuiBind<string> DeityAlignment { get; } = new("deity_alignment");
    public NuiBind<string> DeityDomains { get; } = new("deity_domains");
    public NuiBind<string> AlignmentStatus { get; } = new("alignment_status");
    public NuiBind<Color> AlignmentStatusColor { get; } = new("alignment_color");

    // Player info bindings
    public NuiBind<string> PlayerDeity { get; } = new("player_deity");
    public NuiBind<string> PlayerAlignment { get; } = new("player_alignment");
    public NuiBind<string> PlayerHeader { get; } = new("player_header");

    // Button state bindings
    public NuiBind<bool> CanChangeDeity { get; } = new("can_change_deity");
    public NuiBind<bool> CanPray { get; } = new("can_pray");
    public NuiBind<string> ChangeDeityLabel { get; } = new("change_deity_label");
    public NuiBind<string> ChangeDeityTooltip { get; } = new("change_deity_tooltip");

    public NuiLayout RootLayout()
    {
        Color labelColor = new(30, 20, 12);

        NuiColumn root = new()
        {
            Children = new List<NuiElement>
            {
                // Background
                new NuiRow
                {
                    Width = 0f,
                    Height = 0f,
                    Children = new List<NuiElement>(),
                    DrawList = new List<NuiDrawListItem> { new NuiDrawListImage("ui_bg", new NuiRect(-5f, -5f, 510f, 490f)) }
                },
                new NuiSpacer { Height = 10f },

                // Deity Name Header
                new NuiLabel(DeityName)
                {
                    Height = 30f,
                    Width = 440f,
                    ForegroundColor = labelColor,
                    HorizontalAlign = NuiHAlign.Center
                },
                new NuiSpacer { Height = 5f },

                // Deity Alignment
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer { Width = 10f },
                        new NuiLabel("Alignment:")
                        {
                            Width = 90f,
                            Height = 25f,
                            ForegroundColor = labelColor
                        },
                        new NuiLabel(DeityAlignment)
                        {
                            Width = 250f,
                            Height = 25f,
                            ForegroundColor = labelColor
                        }
                    }
                },
                new NuiSpacer { Height = 5f },

                // Deity Domains
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer { Width = 10f },
                        new NuiLabel("Domains:")
                        {
                            Width = 90f,
                            Height = 25f,
                            ForegroundColor = labelColor
                        },
                        new NuiLabel(DeityDomains)
                        {
                            Width = 330f,
                            Height = 25f,
                            ForegroundColor = labelColor
                        }
                    }
                },
                new NuiSpacer { Height = 15f },

                // Player Info Header
                new NuiLabel(PlayerHeader)
                {
                    Height = 25f,
                    Width = 440f,
                    ForegroundColor = labelColor,
                    HorizontalAlign = NuiHAlign.Center
                },
                new NuiSpacer { Height = 5f },

                // Player Current Deity
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer { Width = 10f },
                        new NuiLabel("Current Deity:")
                        {
                            Width = 130f,
                            Height = 25f,
                            ForegroundColor = labelColor
                        },
                        new NuiLabel(PlayerDeity)
                        {
                            Width = 240f,
                            Height = 25f,
                            ForegroundColor = labelColor
                        }
                    }
                },
                new NuiSpacer { Height = 5f },

                // Player Alignment
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer { Width = 10f },
                        new NuiLabel("Your Alignment:")
                        {
                            Width = 130f,
                            Height = 25f,
                            ForegroundColor = labelColor
                        },
                        new NuiLabel(PlayerAlignment)
                        {
                            Width = 240f,
                            Height = 25f,
                            ForegroundColor = labelColor
                        }
                    }
                },
                new NuiSpacer { Height = 10f },

                // Alignment Status
                new NuiLabel(AlignmentStatus)
                {
                    Height = 30f,
                    Width = 440f,
                    ForegroundColor = AlignmentStatusColor,
                    HorizontalAlign = NuiHAlign.Center
                },
                new NuiSpacer { Height = 15f },

                // Action Buttons
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer{ Width = 150f},
                        new NuiButton(ChangeDeityLabel)
                        {
                            Id = "btn_change_deity",
                            Enabled = CanChangeDeity,
                            Width = 140f,
                            Height = 35f,
                            DisabledTooltip = ChangeDeityTooltip,
                            Tooltip = ChangeDeityTooltip
                        },
                    }
                },
                new NuiSpacer { Height = 10f },

                // Close Button
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer{Width = 150f},
                        new NuiButton("Close")
                        {
                            Id = "btn_close",
                            Width = 140f,
                            Height = 35f
                        }
                    }
                },
                new NuiSpacer { Height = 10f }
            }
        };
        return root;
    }
}
