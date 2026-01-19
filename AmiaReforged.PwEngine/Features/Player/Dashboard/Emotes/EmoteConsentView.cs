using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Emotes;

public sealed class EmoteConsentView : IScryView
{
    public NuiBind<string> ConsentMessage { get; } = new("consent_message");

    public NuiLayout RootLayout()
    {
        NuiColumn root = new()
        {
            Children = new List<NuiElement>
            {
                // Message row
                new NuiLabel(ConsentMessage)
                {
                    Height = 60f,
                    Margin = 10f,
                    VerticalAlign = NuiVAlign.Middle,
                    HorizontalAlign = NuiHAlign.Center
                },

                // Button row
                new NuiRow
                {
                    Height = 40f,
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer { Width = 10f },
                        new NuiButton("Yes")
                        {
                            Id = "btn_consent_yes",
                            Width = 100f,
                            Height = 35f
                        },
                        new NuiSpacer { Width = 10f },
                        new NuiButton("No")
                        {
                            Id = "btn_consent_no",
                            Width = 100f,
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
