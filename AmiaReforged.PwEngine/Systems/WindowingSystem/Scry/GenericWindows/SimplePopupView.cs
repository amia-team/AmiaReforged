using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.WindowingSystem.Scry.StandaloneWindows;

public sealed class SimplePopupView : ScryView<SimplePopupPresenter>
{
    private readonly string _message;
    public sealed override SimplePopupPresenter Presenter { get; protected set; }
    public bool IgnoreButton { get; }
    public NuiBind<bool> IgnoreButtonVisible { get; } = new("ignore_button_visible");

    public SimplePopupView(NwPlayer player, string message, string title, bool ignoreButton = false)
    {
        _message = message;
        IgnoreButton = ignoreButton;
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
                    Element = new NuiText(_message)
                    {
                        Scrollbars = NuiScrollbars.Auto
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
                            Height = 80f,
                            Encouraged = true
                        },
                        new NuiButton("Don't Show Again")
                        {
                            Id = "ignore_button",
                            Width = 120f,
                            Height = 80f,
                            Visible = IgnoreButtonVisible
                        }
                    }
                }
            }
        };
        return popupLayout;
    }
}