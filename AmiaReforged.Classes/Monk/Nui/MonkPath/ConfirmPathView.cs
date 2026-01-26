﻿﻿using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.Classes.Monk.Nui.MonkPath;

public sealed class ConfirmPathView(MonkPathPresenter presenter) : ScryView<MonkPathPresenter>
{
    private readonly NuiBind<string> _pathLabel = new(key: "path_label");
    private readonly NuiBind<string> _pathIcon = new(key: "path_icon");
    private readonly NuiBind<string> _pathText = new(key: "path_text");

    public NuiButton ConfirmPathButton = null!;
    public NuiButton BackButton = null!;


    public override MonkPathPresenter Presenter { get; protected set; } = presenter;

    public override NuiLayout RootLayout() =>
        new NuiColumn
        {
            Children =
            {
                new NuiRow
                {
                    Children =
                    {
                        new NuiImage(_pathIcon)
                        {
                            Height = 64,
                            Width = 64
                        },
                        new NuiLabel(_pathLabel)
                        {
                            VerticalAlign = NuiVAlign.Middle,
                            HorizontalAlign = NuiHAlign.Center,
                            Height = 64,
                            ForegroundColor = new Color(50, 40, 30)
                        }
                    }
                },

                new NuiRow
                {
                    Children =
                    {
                        new NuiText(_pathText)
                        {
                            Height = 450,
                            Scrollbars = NuiScrollbars.None,
                            ForegroundColor = new Color(50, 40, 30)
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

