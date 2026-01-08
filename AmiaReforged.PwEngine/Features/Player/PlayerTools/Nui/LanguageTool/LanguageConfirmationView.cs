using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Action = System.Action;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.LanguageTool;

public class LanguageConfirmationView : ScryView<LanguageConfirmationPresenter>
{
    private const float WindowW = 430f;
    private const float WindowH = 360f;

    public readonly NuiBind<string> ConfirmationMessage = new(key: "confirmation_message");

    public NuiButton ConfirmButton = null!;
    public NuiButton CancelButton = null!;

    public LanguageConfirmationView(NwPlayer player, Action onConfirm, string message)
    {
        Presenter = new LanguageConfirmationPresenter(this, player, onConfirm, message);
    }

    public sealed override LanguageConfirmationPresenter Presenter { get; protected set; }

    public override NuiLayout RootLayout()
    {
        NuiRow bgLayer = new NuiRow
        {
            Width = 0f,
            Height = 0f,
            Children = new List<NuiElement>(),
            DrawList = [new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, WindowW, WindowH))]
        };

        NuiColumn root = new()
        {
            Width = WindowW,
            Height = WindowH,
            Children =
            {
                bgLayer,
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 5f },
                        new NuiText(ConfirmationMessage)
                        {
                            Width = 400f,
                            Height = 200f,
                            Scrollbars = NuiScrollbars.None
                        },
                        new NuiSpacer { Width = 5f }
                    }
                },
                new NuiSpacer { Height = 5f },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer(),
                        new NuiButton("Confirm")
                        {
                            Id = "confirm_button",
                            Width = 120f,
                            Height = 40f,
                            Encouraged = true
                        }.Assign(out ConfirmButton),
                        new NuiSpacer { Width = 20f },
                        new NuiButton("Cancel")
                        {
                            Id = "cancel_button",
                            Width = 120f,
                            Height = 40f
                        }.Assign(out CancelButton),
                        new NuiSpacer()
                    }
                }
            }
        };

        return root;
    }
}

