using AmiaReforged.Classes.Monk.Types;
using AmiaReforged.PwEngine.Systems.WindowingSystem;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.Classes.Monk.Nui.MonkPath;

public sealed class ConfirmPathView : ScryView<ConfirmPathPresenter>
{
    public NuiBind<string> PathLabel = new(key: "path_label");
    public NuiBind<string> PathIcon = new(key: "path_icon");
    public NuiBind<string> PathText = new(key: "path_text");
    public NuiBind<PathType> SelectedPath = new(key: "selected_path");

    public NuiButton ConfirmPathButton = null!;
    public ConfirmPathView(MonkPathPresenter parent, NwPlayer player)
    {
        Parent = parent;
        Presenter = new ConfirmPathPresenter(parent, player, this);
    }

    public MonkPathPresenter Parent;

    public override ConfirmPathPresenter Presenter { get; protected set; }

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
                        new NuiSpacer()
                    }
                }
            }
        };
}

