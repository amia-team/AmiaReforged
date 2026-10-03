using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Nui.Prototype;

/// <summary>Isolated rendering spike. All dimensions are logical NUI units, before client GUI scaling.</summary>
public sealed class CodexPrototypeView : IScryView
{
    public const float WindowWidth = 940f;
    public const float WindowHeight = 650f;
    private static readonly Color Gold = new(242, 196, 113);
    private static readonly Color Ink = new(48, 29, 15);

    public readonly NuiBind<string> CategoryTexture = new("cdxp_category_texture");
    public readonly NuiBind<string> EntryTexture = new("cdxp_entry_texture");
    public readonly NuiBind<bool> EntryEnabled = new("cdxp_entry_enabled");
    public readonly NuiBind<Color> EntryColor = new("cdxp_entry_color");
    public readonly NuiBind<string> DisableLabel = new("cdxp_disable_label");
    public readonly NuiBind<string> Title = new("cdxp_title");
    public readonly NuiBind<string> Header = new("cdxp_header");
    public readonly NuiBind<string> Body = new("cdxp_body");
    public readonly NuiBind<string> Status = new("cdxp_status");
    public readonly NuiBind<string> Events = new("cdxp_events");

    public NuiLayout RootLayout() => new NuiColumn
    {
        Width = WindowWidth, Height = WindowHeight, Margin = 0, Padding = 0,
        Children = [BuildShell()]
    };

    // Drawing directly on the root group did not render on the client. Use a child
    // widget for the shell, as with the category, entry and parchment panels.
    private NuiGroup BuildShell() => new()
    {
        Id = "cdxp_shell",
        Width = WindowWidth, Height = WindowHeight, Margin = 0, Padding = 0,
        Border = false, Scrollbars = NuiScrollbars.None, Scissor = true,
        DrawList =
        [
            Picture("ui_cdx_bg", 24, 24, 892, 602, NuiDrawListItemOrder.Before),
            .. Frame(WindowWidth, WindowHeight, "fr", 28)
        ],
        Element = Inset(BuildContent(), WindowWidth, WindowHeight, 24)
    };

    private NuiColumn BuildContent() => new()
    {
        Width = 892, Height = 602, Margin = 0, Padding = 0,
        Children =
        [
            new NuiRow
            {
                Height = 60, Margin = 0, Padding = 0,
                Children =
                [
                    new NuiImage("ui_cdx_tab_s")
                    {
                        Width = 230, Height = 60, Margin = 0, Padding = 0, ImageAspect = NuiAspect.Stretch,
                        DrawList =
                        [
                            Picture("ui_cdx_i_book", 20, 14, 32, 32),
                            new NuiDrawListText(Gold, new NuiRect(62, 14, 150, 32), "Knowledge")
                            { Order = NuiDrawListItemOrder.After }
                        ]
                    },
                    new NuiLabel(Header)
                    {
                        Width = 614, Height = 60, Margin = 0, ForegroundColor = Gold,
                        HorizontalAlign = NuiHAlign.Center, VerticalAlign = NuiVAlign.Middle
                    },
                    new NuiImage("ui_cdx_close")
                    {
                        Id = "cdxp_close", Width = 48, Height = 60, Margin = 0, Padding = 0,
                        ImageAspect = NuiAspect.Fit, Tooltip = "Close prototype (or repeat ./codex-ui)"
                    }
                ]
            },
            new NuiSpacer { Height = 8, Margin = 0 },
            new NuiRow
            {
                Height = 450, Margin = 0, Padding = 0,
                Children =
                [
                    DarkPanel(160, new NuiColumn
                    {
                        Width = 136, Height = 426, Margin = 0, Padding = 0,
                        Children =
                        [
                            Heading("Categories", 136),
                            new NuiImage(CategoryTexture)
                            {
                                Id = "cdxp_category", Width = 136, Height = 44, Margin = 0, Padding = 0,
                                ImageAspect = NuiAspect.Stretch, Tooltip = "Toggle selected category artwork",
                                DrawList =
                                [
                                    Picture("ui_cdx_i_book", 10, 10, 24, 24),
                                    new NuiDrawListText(Gold, new NuiRect(40, 8, 88, 28), "Knowledge")
                                    { Order = NuiDrawListItemOrder.After }
                                ]
                            },
                            new NuiSpacer { Height = 30, Margin = 0 },
                            Heading("Subregion probe", 136),
                            new NuiRow
                            {
                                Height = 40, Margin = 0, Padding = 0,
                                Children =
                                [
                                    new NuiImage("ui_cdx_fr_tl")
                                    {
                                        Width = 40, Height = 40, Margin = 0, ImageAspect = NuiAspect.Stretch,
                                        Tooltip = "Standalone frame corner (left)"
                                    },
                                    new NuiImage("ui_cdx_outer")
                                    {
                                        Width = 40, Height = 40, Margin = 0, ImageAspect = NuiAspect.Stretch,
                                        ImageRegion = new NuiRect(0, 0, 54, 54),
                                        Tooltip = "Same corner via source-pixel image_region (right)"
                                    }
                                ]
                            },
                            new NuiSpacer { Margin = 0, Padding = 0 }
                        ]
                    }),
                    new NuiSpacer { Width = 12, Margin = 0 },
                    DarkPanel(250, new NuiColumn
                    {
                        Width = 226, Height = 426, Margin = 0, Padding = 0,
                        Children =
                        [
                            Heading("Entries", 226),
                            new NuiImage(EntryTexture)
                            {
                                Id = "cdxp_entry", Width = 226, Height = 54, Margin = 0, Padding = 0,
                                ImageAspect = NuiAspect.Stretch, Enabled = EntryEnabled,
                                Tooltip = "Toggle entry highlight; reload long sample text",
                                DisabledTooltip = "Disabled entry: must not activate",
                                DrawList =
                                [
                                    new NuiDrawListText(EntryColor, new NuiRect(16, 12, 158, 30), "Sample knowledge")
                                    { Order = NuiDrawListItemOrder.After },
                                    new NuiDrawListImage("ui_cdx_i_next", new NuiRect(184, 12, 28, 28))
                                    { Color = EntryColor, Aspect = NuiAspect.Fit, Order = NuiDrawListItemOrder.After }
                                ]
                            },
                            new NuiSpacer { Margin = 0, Padding = 0 },
                            new NuiLabel("Static sample content")
                            { Width = 226, Height = 28, Margin = 0, ForegroundColor = Gold }
                        ]
                    }),
                    new NuiSpacer { Width = 12, Margin = 0 },
                    new NuiGroup
                    {
                        Id = "cdxp_paper", Width = 458, Height = 450, Border = false,
                        Margin = 0, Padding = 0, Scrollbars = NuiScrollbars.None, Scissor = true,
                        DrawList = [Picture("ui_cdx_paper", 0, 0, 458, 450, NuiDrawListItemOrder.Before)],
                        Element = Inset(new NuiColumn
                        {
                            Width = 406, Height = 398, Margin = 0, Padding = 0,
                            Children =
                            [
                                new NuiLabel(Title)
                                {
                                    Width = 406, Height = 32, Margin = 0, ForegroundColor = Ink,
                                    HorizontalAlign = NuiHAlign.Center, VerticalAlign = NuiVAlign.Middle
                                },
                                new NuiText(Body)
                                {
                                    Id = "cdxp_body", Width = 406, Height = 366, Margin = 0, Padding = 0,
                                    ForegroundColor = Ink, Border = false, Scrollbars = NuiScrollbars.Y, Scissor = true
                                }
                            ]
                        }, 458, 450, 26)
                    }
                ]
            },
            new NuiSpacer { Height = 12, Margin = 0 },
            new NuiRow
            {
                Height = 30, Margin = 0, Padding = 0,
                Children =
                [
                    new NuiButton(DisableLabel) { Id = "cdxp_disable", Width = 150, Height = 30, Margin = 0 },
                    new NuiButton("Center") { Id = "cdxp_center", Width = 90, Height = 30, Margin = 0 },
                    new NuiButton("Top left") { Id = "cdxp_corner", Width = 90, Height = 30, Margin = 0 },
                    new NuiButton("Reset counts") { Id = "cdxp_reset", Width = 110, Height = 30, Margin = 0 },
                    new NuiLabel(Status) { Width = 452, Height = 30, Margin = 0, ForegroundColor = Gold }
                ]
            },
            new NuiLabel(Events) { Width = 892, Height = 40, Margin = 0, ForegroundColor = Gold },
            new NuiSpacer { Height = 2, Margin = 0, Padding = 0 }
        ]
    };

    private static NuiLabel Heading(string text, float width) => new(text)
    { Width = width, Height = 32, Margin = 0, ForegroundColor = Gold, VerticalAlign = NuiVAlign.Middle };

    private static NuiGroup DarkPanel(float width, NuiColumn content)
    {
        List<NuiDrawListItem> draw = [Picture("ui_cdx_bg", 0, 0, width, 450, NuiDrawListItemOrder.Before)];
        draw.AddRange(Frame(width, 450, "pn", 18));
        return new NuiGroup
        {
            Width = width, Height = 450, Margin = 0, Padding = 0, Border = false,
            Scrollbars = NuiScrollbars.None, Element = Inset(content, width, 450, 12), DrawList = draw,
            Scissor = true
        };
    }

    // Span padding does not inset children like CSS padding. Keep artwork on the unpadded
    // group and reserve the content inset with real layout elements in both axes.
    private static NuiColumn Inset(NuiElement content, float width, float height, float inset) => new()
    {
        Width = width, Height = height, Margin = 0, Padding = 0,
        Children =
        [
            new NuiSpacer { Height = inset, Margin = 0, Padding = 0 },
            new NuiRow
            {
                Width = width, Height = height - 2 * inset, Margin = 0, Padding = 0,
                Children =
                [
                    new NuiSpacer { Width = inset, Margin = 0, Padding = 0 },
                    content,
                    new NuiSpacer { Width = inset, Margin = 0, Padding = 0 }
                ]
            },
            new NuiSpacer { Height = inset, Margin = 0, Padding = 0 }
        ]
    };

    // Preserve ornament proportions at fixed corners; only the straight edge spans stretch.
    private static List<NuiDrawListItem> Frame(float width, float height, string prefix, float corner) =>
    [
        Picture($"ui_cdx_{prefix}_tl", 0, 0, corner, corner, NuiDrawListItemOrder.Before),
        Picture($"ui_cdx_{prefix}_tr", width - corner, 0, corner, corner, NuiDrawListItemOrder.Before),
        Picture($"ui_cdx_{prefix}_bl", 0, height - corner, corner, corner, NuiDrawListItemOrder.Before),
        Picture($"ui_cdx_{prefix}_br", width - corner, height - corner, corner, corner, NuiDrawListItemOrder.Before),
        Picture($"ui_cdx_{prefix}_top", corner, 0, width - 2 * corner, corner, NuiDrawListItemOrder.Before),
        Picture($"ui_cdx_{prefix}_bot", corner, height - corner, width - 2 * corner, corner, NuiDrawListItemOrder.Before),
        Picture($"ui_cdx_{prefix}_left", 0, corner, corner, height - 2 * corner, NuiDrawListItemOrder.Before),
        Picture($"ui_cdx_{prefix}_right", width - corner, corner, corner, height - 2 * corner, NuiDrawListItemOrder.Before)
    ];

    private static NuiDrawListImage Picture(string resource, float x, float y, float width, float height,
        NuiDrawListItemOrder order = NuiDrawListItemOrder.After) => new(resource, new NuiRect(x, y, width, height))
    { Aspect = NuiAspect.Stretch, Order = order };
}
