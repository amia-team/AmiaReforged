using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.WindowingSystem.Scry.StandaloneWindows;

public sealed class SimplePopupView : ScryView<SimplePopupPresenter>
{
    private readonly string _message;
    public sealed override SimplePopupPresenter Presenter { get; protected set; }

    public SimplePopupView(NwPlayer player, string message, string title)
    {
        _message = message;
        Presenter = new SimplePopupPresenter(player, this, title);
    }

    public override NuiLayout RootLayout()
    {
        NuiColumn popupLayout = new()
        {
            Children =
            {
                new NuiGroup
                {
                    Element = new NuiLabel(_message)
                    {
                        HorizontalAlign = NuiHAlign.Center,
                        VerticalAlign = NuiVAlign.Middle
                    },
                    Border = true,
                    Width = 300,
                    Height = 150,
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer(),
                        new NuiButton("OK")
                        {
                            Id = "ok_button",
                            Width = 80f,
                            Height = 80f
                        },
                        new NuiSpacer()
                    }
                }
            }
        };
        return popupLayout;
    }
}