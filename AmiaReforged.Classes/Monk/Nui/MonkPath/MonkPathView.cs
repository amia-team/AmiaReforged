﻿﻿using AmiaReforged.Classes.Monk.Types;
using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.Classes.Monk.Nui.MonkPath;

public sealed class MonkPathView : ScryView<MonkPathPresenter>
{
    private MonkPathModel Model { get; } = new();

    public readonly NuiBind<PathType> PathBind = new(key: "path_type");
    public readonly NuiBind<bool> IsConfirmViewOpen = new(key: "is_confirm_view_open");
    public readonly NuiBind<string> PathLabel = new(key: "path_label");
    public readonly NuiBind<string> PathIcon = new(key: "path_icon");
    public readonly NuiBind<string> PathText = new(key: "path_text");
    public NuiButton ConfirmPathButton = null!;
    public readonly NuiButton BackButton = null!;

    public MonkPathView(NwPlayer player)
    {
        Presenter = new MonkPathPresenter(this, player);
    }

    public override MonkPathPresenter Presenter { get; protected set; }

    public override NuiLayout RootLayout() =>
        new NuiColumn
        {
            Children =
            {
                new NuiRow
                {
                    Width = 0f,
                    Height = 0f,
                    Children = [],
                    DrawList = [new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, 600f, 640f))]
                },

                new NuiColumn
                {
                    Children = Model.Paths
                        .Select(p => CreatePathRow(p.Type, p.Icon, p.Description))
                        .Cast<NuiElement>()
                        .ToList()
                },

                new NuiColumn
                {
                    Visible = IsConfirmViewOpen,
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
                                    Height = 280f,
                                    Scrollbars = NuiScrollbars.Y
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
                }
            }
        };

    private static NuiRow CreatePathRow(PathType type, string icon, string description)
    {
        NuiBind<bool> glowBind = new($"glow_{type}");

        return new NuiRow
        {
            Children =
            {
                new NuiButtonImage(icon)
                {
                    Id = type.ToString(),
                    Height = 64,
                    Width = 64,
                    Encouraged = glowBind
                },
                new NuiText(description)
                {
                    Height = 64,
                    Width = 400,
                    Border = false,
                    Scrollbars = NuiScrollbars.None
                }
            }
        };
    }
}
