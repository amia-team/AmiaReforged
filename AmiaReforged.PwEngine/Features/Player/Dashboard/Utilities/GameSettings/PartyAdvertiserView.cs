using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Utilities.GameSettings;

public sealed class PartyAdvertiserView : IScryView
{
    public NuiBind<bool> IsAdvertising { get; } = new("is_advertising");
    public NuiBind<string> AdvertiseButtonLabel { get; } = new("advertise_label");
    public NuiBind<string> PartyListText { get; } = new("party_list");

    // Toggle button labels (show [X] or [ ] prefix)
    public NuiBind<string> ShowNameLabel { get; } = new("show_name_label");
    public NuiBind<string> ShowLevelLabel { get; } = new("show_level_label");
    public NuiBind<string> ShowBuildLabel { get; } = new("show_build_label");
    public NuiBind<string> ShowAreaLabel { get; } = new("show_area_label");
    public NuiBind<string> LookingForRpLabel { get; } = new("looking_rp_label");
    public NuiBind<string> LookingForHuntLabel { get; } = new("looking_hunt_label");

    // Custom message input
    public NuiBind<string> CustomMessage { get; } = new("custom_message");

    public NuiLayout RootLayout()
    {
        Color labelColor = new(30, 20, 12);

        NuiColumn root = new()
        {
            Children = new List<NuiElement>
            {
                new NuiRow
                {
                    Width = 0f,
                    Height = 0f,
                    Children = new List<NuiElement>(),
                    DrawList = new List<NuiDrawListItem> { new NuiDrawListImage("ui_bg_party", new NuiRect(0f, 0f, 500f, 650f)) }
                },
                new NuiSpacer { Height = 10f },
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer { Width = 140f },
                        new NuiLabel("Party Advertiser")
                        {
                            Height = 25f,
                            Width = 200f,
                            HorizontalAlign = NuiHAlign.Center,
                            ForegroundColor = labelColor
                        }
                    }
                },
                new NuiSpacer { Height = 5f },

                // Options section
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer { Width = 185f },
                        new NuiLabel("Display Options:")
                        {
                            Height = 20f,
                            Width = 150f,
                            HorizontalAlign = NuiHAlign.Left,
                            ForegroundColor = labelColor
                        }
                    }
                },
                new NuiRow
                {
                    Height = 30f,
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer { Width = 90f },
                        new NuiButton(ShowNameLabel) { Id = "btn_toggle_name", Width = 80f, Height = 25f },
                        new NuiButton(ShowLevelLabel) { Id = "btn_toggle_level", Width = 80f, Height = 25f },
                        new NuiButton(ShowBuildLabel) { Id = "btn_toggle_build", Width = 80f, Height = 25f },
                        new NuiButton(ShowAreaLabel) { Id = "btn_toggle_area", Width = 80f, Height = 25f }
                    }
                },
                new NuiRow
                {
                    Height = 30f,
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer { Width = 155f },
                        new NuiButton(LookingForRpLabel) { Id = "btn_toggle_rp", Width = 100f, Height = 25f },
                        new NuiButton(LookingForHuntLabel) { Id = "btn_toggle_hunt", Width = 100f, Height = 25f }
                    }
                },
                new NuiSpacer { Height = 5f },

                // Custom message
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer { Width = 90f },
                        new NuiLabel("Custom Message (optional, max 60 chars):")
                        {
                            Height = 20f,
                            Width = 320f,
                            HorizontalAlign = NuiHAlign.Left,
                            ForegroundColor = labelColor
                        }
                    }
                },
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer { Width = 90f },
                        new NuiTextEdit("Enter custom message...", CustomMessage, 60, false)
                        {
                            Height = 30f,
                            Width = 330f
                        }
                    }
                },
                new NuiSpacer { Height = 10f },

                // Add/Remove button
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer { Width = 170f },
                        new NuiButton(AdvertiseButtonLabel)
                        {
                            Id = "btn_toggle_advertise",
                            Height = 35f,
                            Width = 180f
                        }
                    }
                },
                new NuiSpacer { Height = 10f },

                // Party list section
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer { Width = 140f },
                        new NuiLabel("Players Looking for a Party:")
                        {
                            Height = 20f,
                            Width = 250f,
                            HorizontalAlign = NuiHAlign.Center,
                            ForegroundColor = labelColor
                        }
                    }
                },
                new NuiRow
                {
                    Height = 30f,
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer { Width = 90f },
                        new NuiLabel("Filter by:") { Width = 70f, Height = 25f, ForegroundColor = labelColor, VerticalAlign = NuiVAlign.Middle },
                        new NuiButton("All")
                        {
                            Id = "btn_filter_all",
                            Width = 60f,
                            Height = 25f
                        },
                        new NuiButton("RP")
                        {
                            Id = "btn_filter_rp",
                            Width = 60f,
                            Height = 25f
                        },
                        new NuiButton("Hunt")
                        {
                            Id = "btn_filter_hunt",
                            Width = 60f,
                            Height = 25f
                        }
                    }
                },
                new NuiSpacer { Height = 5f },
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer { Width = 80f },
                        new NuiText(PartyListText)
                        {
                            Height = 150f,
                            Width = 390f,
                            ForegroundColor = labelColor,
                            Border = false,
                            Scrollbars = NuiScrollbars.Y
                        }
                    }
                },
                new NuiSpacer { Height = 10f },
                new NuiRow
                {
                    Height = 35f,
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer { Width = 140f },
                        new NuiButton("Refresh")
                        {
                            Id = "btn_refresh",
                            Width = 100f,
                            Height = 30f
                        },
                        new NuiSpacer { Width = 10f },
                        new NuiButton("Close")
                        {
                            Id = "btn_close",
                            Width = 100f,
                            Height = 30f
                        }
                    }
                },
                new NuiSpacer { Height = 50f }
            }
        };
        return root;
    }
}
