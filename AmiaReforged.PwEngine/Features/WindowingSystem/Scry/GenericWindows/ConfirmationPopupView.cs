using Anvil.API;
using Action = System.Action;

namespace AmiaReforged.PwEngine.Features.WindowingSystem.Scry.GenericWindows;

/// <summary>
/// View for a confirmation popup with Confirm and Cancel buttons.
/// </summary>
public sealed class ConfirmationPopupView : ScryView<ConfirmationPopupPresenter>
{
    public const string ConfirmButtonId = "confirm_button";
    public const string CancelButtonId = "cancel_button";
    
    private readonly string _message;

    public ConfirmationPopupView(NwPlayer player, string message, string title, Action onConfirm, Action onCancel)
    {
        _message = message;
        Presenter = new ConfirmationPopupPresenter(player, this, title, onConfirm, onCancel);
    }

    public override ConfirmationPopupPresenter Presenter { get; protected set; }

    public override NuiLayout RootLayout()
    {
        NuiColumn popupLayout = new()
        {
            Children =
            {
                new NuiRow
                {
                    Width = 0f,
                    Height = 0f,
                    Children = new List<NuiElement>(),
                    DrawList = [new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, 450f, 350f))]
                },
                new NuiGroup
                {
                    Element = new NuiText(_message)
                    {
                        Scrollbars = NuiScrollbars.Auto
                    },
                    Border = true,
                    Width = 400,
                    Height = 200
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer(),
                        new NuiButton(label: "Confirm")
                        {
                            Id = ConfirmButtonId,
                            Width = 120f,
                            Height = 60f,
                            Encouraged = true
                        },
                        new NuiSpacer { Width = 20f },
                        new NuiButton(label: "Cancel")
                        {
                            Id = CancelButtonId,
                            Width = 120f,
                            Height = 60f
                        },
                        new NuiSpacer()
                    }
                }
            }
        };
        return popupLayout;
    }
}
