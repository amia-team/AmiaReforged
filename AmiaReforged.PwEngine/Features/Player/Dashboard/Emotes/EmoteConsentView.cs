using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Emotes;

public sealed class EmoteConsentView : IScryView
{
    public NuiBind<string> ConsentMessage { get; } = new("consent_message");
    public NuiBind<string> RequesterPortrait { get; } = new("requester_portrait");

    public NuiLayout RootLayout()
    {
        NuiRow bgLayer = new()
        {
            Width = 0f,
            Height = 0f,
            Children = new List<NuiElement>(),
            DrawList = new List<NuiDrawListItem> { new NuiDrawListImage("ui_bg_emote", new NuiRect(0f, 0f, 300, 300)) }
        };
        NuiColumn root = new()
        {
            Children = new List<NuiElement>
            {
                bgLayer,
                new NuiSpacer { Height = 10f },
                // Portrait image
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer(),
                        new NuiImage(RequesterPortrait)
                        {
                            Width = 64f,
                            Height = 100f,
                            HorizontalAlign = NuiHAlign.Center,
                            ImageAspect = NuiAspect.Fit
                        },
                        new NuiSpacer()
                    }
                },
                new NuiSpacer { Height = 10f },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 10f },
                        // Message text box
                        new NuiText(ConsentMessage)
                        {
                            Height = 80f,
                            Width = 250f,
                            Margin = 10f,
                            Scrollbars = NuiScrollbars.None
                        }
                    }
                },

                // Button row
                new NuiRow
                {
                    Height = 40f,
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer { Width = 60f },
                        new NuiButton("Sure!")
                        {
                            Id = "btn_consent_yes",
                            Width = 70f,
                            Height = 35f
                        },
                        new NuiSpacer { Width = 10f },
                        new NuiButton("Never!")
                        {
                            Id = "btn_consent_no",
                            Width = 70f,
                            Height = 35f
                        },
                        new NuiSpacer { Width = 10f }
                    }
                }
            }
        };
        return root;
    }
}
