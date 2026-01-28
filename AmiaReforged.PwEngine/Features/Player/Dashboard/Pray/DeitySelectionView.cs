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

    // Button state bindings
    public NuiBind<bool> CanChangeDeity { get; } = new("can_change_deity");
    public NuiBind<bool> CanPray { get; } = new("can_pray");
    public NuiBind<string> ChangeDeityLabel { get; } = new("change_deity_label");

    public NuiLayout RootLayout()
    {
        Color labelColor = new(180, 180, 180);
        Color headerColor = new(220, 220, 220);

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
                    DrawList = new List<NuiDrawListItem> { new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, 380f, 400f)) }
                },
                new NuiSpacer { Height = 10f },

                // Deity Name Header
                new NuiLabel(DeityName)
                {
                    Height = 30f,
                    HorizontalAlign = NuiHAlign.Center,
                    ForegroundColor = headerColor
                },
                new NuiSpacer { Height = 5f },

                // Deity Alignment
                new NuiRow
                {
                    Height = 25f,
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer { Width = 15f },
                        new NuiLabel("Alignment:")
                        {
                            Width = 80f,
                            ForegroundColor = labelColor
                        },
                        new NuiLabel(DeityAlignment)
                        {
                            Width = 260f
                        },
                        new NuiSpacer { Width = 15f }
                    }
                },

                // Deity Domains
                new NuiRow
                {
                    Height = 25f,
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer { Width = 15f },
                        new NuiLabel("Domains:")
                        {
                            Width = 80f,
                            ForegroundColor = labelColor
                        },
                        new NuiLabel(DeityDomains)
                        {
                            Width = 260f
                        },
                        new NuiSpacer { Width = 15f }
                    }
                },
                new NuiSpacer { Height = 15f },

                // Separator
                new NuiRow
                {
                    Height = 2f,
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer { Width = 20f },
                        new NuiLabel("─────────────────────────────────")
                        {
                            ForegroundColor = new Color(100, 100, 100)
                        },
                        new NuiSpacer { Width = 20f }
                    }
                },
                new NuiSpacer { Height = 10f },

                // Player Info Header
                new NuiLabel("Your Character")
                {
                    Height = 25f,
                    HorizontalAlign = NuiHAlign.Center,
                    ForegroundColor = headerColor
                },
                new NuiSpacer { Height = 5f },

                // Player Current Deity
                new NuiRow
                {
                    Height = 25f,
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer { Width = 15f },
                        new NuiLabel("Current Deity:")
                        {
                            Width = 100f,
                            ForegroundColor = labelColor
                        },
                        new NuiLabel(PlayerDeity)
                        {
                            Width = 240f
                        },
                        new NuiSpacer { Width = 15f }
                    }
                },

                // Player Alignment
                new NuiRow
                {
                    Height = 25f,
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer { Width = 15f },
                        new NuiLabel("Your Alignment:")
                        {
                            Width = 100f,
                            ForegroundColor = labelColor
                        },
                        new NuiLabel(PlayerAlignment)
                        {
                            Width = 240f
                        },
                        new NuiSpacer { Width = 15f }
                    }
                },
                new NuiSpacer { Height = 10f },

                // Alignment Status (whether player can worship this deity)
                new NuiLabel(AlignmentStatus)
                {
                    Height = 30f,
                    HorizontalAlign = NuiHAlign.Center,
                    ForegroundColor = AlignmentStatusColor
                },
                new NuiSpacer { Height = 15f },

                // Action Buttons
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer(),
                        new NuiButton("Pray")
                        {
                            Id = "btn_pray",
                            Enabled = CanPray,
                            Width = 100f,
                            Height = 35f,
                            Tooltip = "Pray to this deity"
                        },
                        new NuiSpacer { Width = 10f },
                        new NuiButton(ChangeDeityLabel)
                        {
                            Id = "btn_change_deity",
                            Enabled = CanChangeDeity,
                            Width = 140f,
                            Height = 35f,
                            Tooltip = "Set this deity as your patron"
                        },
                        new NuiSpacer()
                    }
                },
                new NuiSpacer { Height = 10f },

                // Close Button
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer(),
                        new NuiButton("Close")
                        {
                            Id = "btn_close",
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
