using AmiaReforged.Classes.Monk.Types;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;
using AmiaReforged.PwEngine.Systems.WindowingSystem;

namespace AmiaReforged.Classes.Monk.Nui.MonkPath;

public sealed class MonkPathView : ScryView<MonkPathPresenter>
{
    public NuiBind<PathType> PathBind = new(key: "path_type");
    public NuiBind<bool> IsConfirmViewOpen = new(key: "is_confirm_view_open");
    public NuiBind<string> PathLabel = new(key: "path_label");
    public NuiBind<string> PathIcon = new(key: "path_icon");
    public NuiBind<string> PathText = new(key: "path_text");
    public NuiButton ConfirmPathButton = null!;
    public NuiButton BackButton = null!;

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
                                    Id = PathType.CrashingMeteor.ToString(),
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
                                    Id = PathType.EchoingValley.ToString(),
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
                                    Id = PathType.FickleStrand.ToString(),
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
                                    Id = PathType.FloatingLeaf.ToString(),
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
                                    Id = PathType.IroncladBull.ToString(),
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
                                    Id = PathType.SplinteredChalice.ToString(),
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
                                    Id = PathType.SwingingCenser.ToString(),
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
