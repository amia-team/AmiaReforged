﻿﻿using AmiaReforged.Classes.Monk.Types;
using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.Classes.Monk.Nui.MonkPath;

public sealed class MonkPathView : ScryView<MonkPathPresenter>
{
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
                new NuiColumn
                {
                    Children =
                    {
                        new NuiRow
                        {
                            Children =
                            {
                                new NuiButtonImage(MonkPathNuiElements.CrashingMeteorIcon)
                                {
                                    Id = nameof(PathType.CrashingMeteor),
                                    Height = 64,
                                    Width = 64
                                },
                                new NuiText(MonkPathNuiElements.CrashingMeteorDescription)
                                {
                                    Border = false,
                                    Scrollbars = NuiScrollbars.None
                                }
                            }
                        },

                        new NuiRow
                        {
                            Children =
                            {
                                new NuiButtonImage(MonkPathNuiElements.EchoingValleyIcon)
                                {
                                    Id = nameof(PathType.EchoingValley),
                                    Height = 64,
                                    Width = 64
                                },
                                new NuiText(text: MonkPathNuiElements.EchoingValleyDescription)
                                {
                                    Border = false,
                                    Scrollbars = NuiScrollbars.None
                                }
                            }
                        },

                        new NuiRow
                        {
                            Children =
                            {
                                new NuiButtonImage(MonkPathNuiElements.FickleStrandIcon)
                                {
                                    Id = nameof(PathType.FickleStrand),
                                    Height = 64,
                                    Width = 64
                                },
                                new NuiText(text: MonkPathNuiElements.FickleStrandDescription)
                                {
                                    Border = false,
                                    Scrollbars = NuiScrollbars.None
                                }
                            }
                        },

                        new NuiRow
                        {
                            Children =
                            {
                                new NuiButtonImage(MonkPathNuiElements.FloatingLeafIcon)
                                {
                                    Id = nameof(PathType.FloatingLeaf),
                                    Height = 64,
                                    Width = 64
                                },
                                new NuiText(text: MonkPathNuiElements.FloatingLeafDescription)
                                {
                                    Border = false,
                                    Scrollbars = NuiScrollbars.None
                                }
                            }
                        },

                        new NuiRow
                        {
                            Children =
                            {
                                new NuiButtonImage(MonkPathNuiElements.IroncladBullIcon)
                                {
                                    Id = nameof(PathType.IroncladBull),
                                    Height = 64,
                                    Width = 64
                                },
                                new NuiText(text: MonkPathNuiElements.IroncladBullDescription)
                                {
                                    Border = false,
                                    Scrollbars = NuiScrollbars.None
                                }
                            }
                        },

                        new NuiRow
                        {
                            Children =
                            {
                                new NuiButtonImage(MonkPathNuiElements.SplinteredChaliceIcon)
                                {
                                    Id = nameof(PathType.SplinteredChalice),
                                    Height = 64,
                                    Width = 64
                                },
                                new NuiText(text: MonkPathNuiElements.SplinteredChaliceDescription)
                                {
                                    Border = false,
                                    Scrollbars = NuiScrollbars.None
                                }
                            }
                        },

                        new NuiRow
                        {
                            Children =
                            {
                                new NuiButtonImage(MonkPathNuiElements.SwingingCenserIcon)
                                {
                                    Id = nameof(PathType.SwingingCenser),
                                    Height = 64,
                                    Width = 64
                                },
                                new NuiText(text: MonkPathNuiElements.SwingingCenserDescription)
                                {
                                    Border = false,
                                    Scrollbars = NuiScrollbars.None
                                }
                            }
                        }
                    }
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
}
