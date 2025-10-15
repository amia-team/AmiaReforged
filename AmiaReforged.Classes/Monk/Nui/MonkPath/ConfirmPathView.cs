using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.Classes.Monk.Nui.MonkPath;

public sealed class ConfirmPathView : ScryView<MonkPathPresenter>
{
    public NuiBind<string> PathLabel = new(key: "path_label");
    public NuiBind<string> PathIcon = new(key: "path_icon");
    public NuiBind<string> PathText = new(key: "path_text");

    public NuiButton ConfirmPathButton = null!;
    public NuiButton BackButton = null!;
    public ConfirmPathView(MonkPathPresenter presenter)
    {
        Presenter = presenter;
    }


    public override MonkPathPresenter Presenter { get; protected set; }

    public override NuiLayout RootLayout() =>
        new NuiColumn
        {
            Children =
            {
                new NuiRow
                {
                    Children =
                    {
                        new NuiImage(PathIcon)
                        {
                            Height = 64,
                            Width = 64
                        },
                        new NuiLabel(PathLabel)
                        {
                            VerticalAlign = NuiVAlign.Middle,
                            HorizontalAlign = NuiHAlign.Center,
                            Height = 64
                        }
                    }
                },

                new NuiRow
                {
                    Children =
                    {
                        new NuiText(PathText)
                        {
                            Height = 450,
                            Scrollbars = NuiScrollbars.None
                        }
                    }
                },

                new NuiRow
                {
                    Children =
                    {
                        new NuiButton("Confirm Path")
                        {
                            Id = "confirm_button",
                            Width = 120f
                        }.Assign(out ConfirmPathButton),
                        new NuiSpacer(),
                        new NuiButton("Back")
                        {
                            Id = "back_button",
                            Width = 120f
                        }.Assign(out BackButton),
                    }
                }
            }
        };
}

