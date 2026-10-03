using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Nui.Player;

/// <summary>Graphical Codex shell. Dimensions are logical NUI units before client GUI scaling.</summary>
public sealed class PlayerCodexView : ScryView<PlayerCodexPresenter>
{
    public const float WindowW = 940f;
    public const float WindowH = 650f;
    public const int EntriesPerPage = 8;
    public const int NotesPerPage = 6;
    private const float BodyH = 450f;
    private const float CategoryW = 136f;
    private const float EntryW = 226f;
    private const float DetailW = 406f;
    public static readonly Color Gold = new(242, 196, 113);
    public static readonly Color Muted = new(110, 100, 80);
    private static readonly Color Ink = new(48, 29, 15);

    public NuiGroup CategoryGroup = null!;
    public NuiGroup EntryListGroup = null!;
    public NuiGroup DetailGroup = null!;
    public readonly NuiBind<NuiRect> Geometry = new("codex_geometry");
    public readonly NuiBind<Color> ControlColor = new("codex_control_color");
    public readonly Dictionary<CodexTab, NuiBind<string>> TabTextures = new();
    public readonly HashSet<string> ImageActionIds = new();
    public readonly NuiBind<string> NoteTitle = new("codex_note_title");
    public readonly NuiBind<string> NoteContent = new("codex_note_content");
    public readonly NuiBind<int> NoteCategorySelection = new("codex_note_category");
    public readonly NuiBind<string> NoteSearch = new("codex_note_search");
    public readonly NuiBind<string> NoteEditorTitle = new("codex_note_editor_title");
    public readonly NuiBind<string> Status = new("codex_status");
    public readonly NuiBind<bool> CanInteract = new("codex_can_interact");
    public readonly NuiBind<bool> CanCloseWindow = new("codex_can_close");
    public readonly NuiBind<bool> ShowNoteActions = new("codex_show_note_actions");
    public readonly NuiBind<bool> ShowConfirmation = new("codex_show_confirmation");
    public readonly NuiBind<string> ConfirmLabel = new("codex_confirm_label");
    public readonly NuiBind<string> DetailTitle = new("codex_detail_title");
    public readonly NuiBind<string> DetailBody = new("codex_detail_body");
    public readonly NuiBind<string> ProficiencyLevelText = new("codex_prof_level");
    public readonly NuiBind<string> IndustryName = new("codex_industry_name");
    public readonly NuiBind<bool> ShowProficiency = new("codex_show_proficiency");
    public readonly NuiBind<float> ProficiencyProgressValue = new("codex_prof_progress");
    public readonly NuiBind<string> ProficiencyProgressLabel = new("codex_prof_label");
    public readonly NuiBind<string> PageInfo = new("codex_page_info");
    public readonly NuiBind<bool> ShowPrevPage = new("codex_show_prev");
    public readonly NuiBind<bool> ShowNextPage = new("codex_show_next");
    public readonly List<NuiBind<string>> EntryNames = new();
    public readonly List<NuiBind<string>> EntrySubtitles = new();
    public readonly List<NuiBind<bool>> EntryRowVisible = new();
    public readonly List<NuiBind<string>> EntryTextures = new();
    public readonly List<NuiBind<string>> EntryTooltips = new();

    public PlayerCodexView(NwPlayer player)
    {
        for (int i = 0; i < EntriesPerPage; i++)
        {
            EntryNames.Add(new NuiBind<string>($"entry_name_{i}"));
            EntrySubtitles.Add(new NuiBind<string>($"entry_sub_{i}"));
            EntryRowVisible.Add(new NuiBind<bool>($"entry_vis_{i}"));
            EntryTextures.Add(new NuiBind<string>($"entry_texture_{i}"));
            EntryTooltips.Add(new NuiBind<string>($"entry_tooltip_{i}"));
        }
        foreach (CodexTab tab in Enum.GetValues<CodexTab>())
            TabTextures.Add(tab, new NuiBind<string>($"codex_tab_texture_{tab.ToString().ToLowerInvariant()}"));

        Presenter = new PlayerCodexPresenter(this, player);
        AnvilCore.GetService<InjectionService>()!.Inject(Presenter);
    }

    public override PlayerCodexPresenter Presenter { get; protected set; }

    public override NuiLayout RootLayout()
    {
        ImageActionIds.Clear();
        return new NuiGroup
        {
            Width = WindowW, Height = WindowH, Margin = 0, Padding = 0,
            Border = false, Scrollbars = NuiScrollbars.None,
            Element = new NuiColumn
            {
                Width = WindowW, Height = WindowH, Margin = 0, Padding = 0,
                Children =
                [
                    new NuiGroup
                    {
                        Id = "codex_shell", Width = WindowW, Height = WindowH,
                        Margin = 0, Padding = 0, Border = false, Scrollbars = NuiScrollbars.None, Scissor = true,
                        DrawList = [Picture("ui_cdx_bg", 8, 8, WindowW - 16, WindowH - 16), .. Frame(WindowW, WindowH, "fr", 28)],
                        Element = Inset(new NuiColumn
                        {
                            Width = 892, Height = 602, Margin = 0, Padding = 0,
                            Children =
                            [
                                BuildTabBar(),
                                new NuiSpacer { Height = 8, Margin = 0 },
                                new NuiRow
                                {
                                    Height = BodyH, Margin = 0, Padding = 0,
                                    Children =
                                    [
                                        DarkPanel(160, new NuiGroup
                                        {
                                            Id = "grp_categories", Width = CategoryW, Height = 426,
                                            Margin = 0, Padding = 0, Border = false, Scrollbars = NuiScrollbars.None, Scissor = true,
                                            Element = new NuiColumn { Margin = 0, Padding = 0, Children = [new NuiSpacer()] }
                                        }.Assign(out CategoryGroup)),
                                        new NuiSpacer { Width = 12, Margin = 0 },
                                        DarkPanel(250, new NuiGroup
                                        {
                                            Id = "grp_entry_list", Width = EntryW, Height = 426,
                                            Margin = 0, Padding = 0, Border = false, Scrollbars = NuiScrollbars.None, Scissor = true,
                                            Element = BuildEntryListInner()
                                        }.Assign(out EntryListGroup)),
                                        new NuiSpacer { Width = 12, Margin = 0 },
                                        new NuiGroup
                                        {
                                            Id = "codex_paper", Width = 458, Height = BodyH,
                                            Margin = 0, Padding = 0, Border = false, Scrollbars = NuiScrollbars.None, Scissor = true,
                                            DrawList = [Picture("ui_cdx_paper", 0, 0, 458, BodyH)],
                                            Element = Inset(new NuiGroup
                                            {
                                                Id = "grp_detail", Width = DetailW, Height = 398,
                                                Margin = 0, Padding = 0, Border = false, Scrollbars = NuiScrollbars.None, Scissor = true,
                                                Element = BuildDetailContent()
                                            }.Assign(out DetailGroup), 458, BodyH, 26)
                                        }
                                    ]
                                },
                                new NuiSpacer { Height = 12, Margin = 0 },
                                BuildFooter()
                            ]
                        }, WindowW, WindowH, 24)
                    }
                ]
            }
        };
    }

    private NuiRow BuildTabBar()
    {
        List<NuiElement> tabs = new();
        foreach (CodexTab tab in Enum.GetValues<CodexTab>())
        {
            if (tabs.Count > 0) tabs.Add(new NuiSpacer { Width = 4, Margin = 0 });
            tabs.Add(ImageControl($"tab_{tab.ToString().ToLowerInvariant()}", TabTextures[tab], tab.ToString(),
                136, 60, $"Open {tab}", TabGlyph(tab), 24));
        }
        tabs.Add(new NuiSpacer { Width = 8, Margin = 0 });
        tabs.Add(ImageControl("codex_close", "ui_cdx_close", null, 48, 60,
            "Close Codex; unsaved changes require confirmation"));
        return new NuiRow { Width = 892, Height = 60, Margin = 0, Padding = 0, Children = tabs };
    }

    private NuiColumn BuildFooter() => new()
    {
        Width = 892, Height = 72, Margin = 0, Padding = 0,
        Children =
        [
            new NuiLabel(Status)
            { Width = 892, Height = 32, Margin = 0, ForegroundColor = Gold, VerticalAlign = NuiVAlign.Middle },
            new NuiSpacer { Height = 8, Margin = 0 },
            new NuiRow
            {
                Width = 892, Height = 32, Margin = 0, Padding = 0,
                Children =
                [
                    ImageControl("codex_center", "ui_cdx_ent_n_v2", "Center", 88, 32, "Center Codex"),
                    new NuiSpacer { Width = 8, Margin = 0 },
                    ImageControl("codex_top_left", "ui_cdx_ent_n_v2", "Top left", 88, 32, "Move Codex to the top left"),
                    new NuiSpacer { Margin = 0 },
                    ImageControl("codex_confirm", "ui_cdx_ent_n_v2", ConfirmLabel, 100, 32,
                        "Confirm the pending action", visible: ShowConfirmation),
                    new NuiSpacer { Width = 8, Margin = 0 },
                    ImageControl("codex_keep", "ui_cdx_ent_n_v2", "Keep", 70, 32,
                        "Keep the current note or draft", visible: ShowConfirmation)
                ]
            }
        ]
    };

    public NuiColumn BuildCategoryColumn(CodexTab tab, string selected, params (string Label, string Id)[] categories)
    {
        ImageActionIds.RemoveWhere(id => id.StartsWith("cat_", StringComparison.Ordinal));
        const float listHeight = 400;
        const float rowHeight = 32;
        const float rowGap = 1;
        float contentHeight = categories.Length * (rowHeight + rowGap);
        bool scroll = contentHeight > listHeight;
        // Native Y scrolling reserves space inside the group. Leave a separate gutter
        // rather than drawing fixed-width controls underneath its scrollbar.
        float controlWidth = CategoryW - (scroll ? 24 : 0);
        List<NuiElement> rows = new();
        foreach ((string label, string id) in categories)
        {
            rows.Add(ImageControl($"cat_{id}",
                string.Equals(id, selected, StringComparison.OrdinalIgnoreCase) ? "ui_cdx_cat_s_v2" : "ui_cdx_cat_n_v2",
                label, controlWidth, rowHeight, $"Show {label}", TabGlyph(tab), 18));
            rows.Add(new NuiSpacer { Height = rowGap, Margin = 0 });
        }
        return new NuiColumn
        {
            Width = CategoryW, Height = 426, Margin = 0, Padding = 0,
            Children =
            [
                Heading(tab.ToString(), CategoryW, 26),
                new NuiGroup
                {
                    Width = CategoryW, Height = listHeight, Margin = 0, Padding = 0, Border = false, Scissor = true,
                    Scrollbars = scroll ? NuiScrollbars.Y : NuiScrollbars.None,
                    Element = new NuiColumn
                    { Width = controlWidth, Height = contentHeight, Margin = 0, Padding = 0, Children = rows }
                }
            ]
        };
    }

    public NuiColumn BuildDetailContent() => new()
    {
        Width = DetailW, Height = 398, Margin = 0, Padding = 0,
        Children =
        [
            new NuiLabel(DetailTitle)
            {
                Width = DetailW, Height = 32, Margin = 0, ForegroundColor = Ink, Tooltip = DetailTitle,
                HorizontalAlign = NuiHAlign.Center, VerticalAlign = NuiVAlign.Middle
            },
            // Reserve the action strip so showing note controls does not resize the reader.
            new NuiGroup
            {
                Width = DetailW, Height = 32, Margin = 0, Padding = 0, Border = false, Scrollbars = NuiScrollbars.None,
                Element = new NuiRow
                {
                    Width = DetailW, Height = 32, Margin = 0, Padding = 0, Visible = ShowNoteActions,
                    Children =
                    [
                        new NuiSpacer { Margin = 0 },
                        ImageControl("note_edit", "ui_cdx_ent_n_v2", "Edit", 96, 32, "Edit this personal note"),
                        new NuiSpacer { Width = 8, Margin = 0 },
                        ImageControl("note_delete", "ui_cdx_ent_n_v2", "Delete", 96, 32, "Delete this personal note; confirmation required")
                    ]
                }
            },
            new NuiText(DetailBody)
            {
                Id = "codex_detail_body", Width = DetailW, Height = 334, Margin = 0, Padding = 0, ForegroundColor = Ink,
                Border = false, Scrollbars = NuiScrollbars.Y, Scissor = true
            }
        ]
    };

    public NuiColumn BuildNoteEditor() => new()
    {
        Width = DetailW, Height = 398, Margin = 0, Padding = 0,
        Children =
        [
            new NuiLabel(NoteEditorTitle)
            { Width = DetailW, Height = 26, Margin = 0, ForegroundColor = Ink, HorizontalAlign = NuiHAlign.Center },
            new NuiSpacer { Height = 4, Margin = 0 },
            InputFrame(new NuiTextEdit("Title (optional)", NoteTitle, CodexNoteEntry.MaxTitleLength, false)
            {
                Id = "note_title_input", Width = 394, Height = 32, Margin = 0, Enabled = CanInteract,
                ForegroundColor = ControlColor, Tooltip = $"Optional title; up to {CodexNoteEntry.MaxTitleLength} characters"
            }, DetailW, 44),
            new NuiSpacer { Height = 6, Margin = 0 },
            InputFrame(new NuiRow
            {
                Width = 394, Height = 32, Margin = 0, Padding = 0,
                Children =
                [
                    new NuiLabel("Category")
                    {
                        Width = 78, Height = 32, Margin = 0, ForegroundColor = ControlColor,
                        VerticalAlign = NuiVAlign.Middle, Tooltip = "Personal note category"
                    },
                    new NuiSpacer { Width = 8, Margin = 0 },
                    // Combo and tooltip use the same native popup; keep help on the label.
                    new NuiCombo
                    {
                        Id = "note_category", Width = 308, Height = 32, Margin = 0, Enabled = CanInteract,
                        ForegroundColor = ControlColor, Selected = NoteCategorySelection,
                        Entries = new List<NuiComboEntry>
                        {
                            new("General", (int)NoteCategory.General), new("Quest", (int)NoteCategory.Quest),
                            new("Character", (int)NoteCategory.Character), new("Location", (int)NoteCategory.Location)
                        }
                    }
                ]
            }, DetailW, 44),
            new NuiSpacer { Height = 6, Margin = 0 },
            InputFrame(new NuiTextEdit("Write your note", NoteContent, CodexNoteEntry.MaxPlayerContentLength, true)
            {
                Id = "note_content_input", Width = 394, Height = 216, Margin = 0, WordWrap = true,
                Enabled = CanInteract, ForegroundColor = ControlColor,
                Tooltip = $"Up to {CodexNoteEntry.MaxPlayerContentLength} characters; scroll to read longer notes"
            }, DetailW, 228),
            new NuiSpacer { Height = 6, Margin = 0 },
            new NuiRow
            {
                Width = DetailW, Height = 34, Margin = 0, Padding = 0,
                Children =
                [
                    ImageControl("note_save", "ui_cdx_ent_n_v2", "Save", 196, 34, "Save this note"),
                    new NuiSpacer { Width = 14, Margin = 0 },
                    ImageControl("note_cancel", "ui_cdx_ent_n_v2", "Cancel", 196, 34, "Cancel editing; unsaved changes require confirmation")
                ]
            }
        ]
    };

    public NuiColumn BuildNotesEntryList()
    {
        List<NuiElement> children =
        [
            ImageControl("note_new", "ui_cdx_ent_n_v2", "New Note", EntryW, 32, "Write a personal note", "ui_cdx_i_scroll", 20),
            new NuiSpacer { Height = 6, Margin = 0 },
            InputFrame(new NuiTextEdit("Search notes", NoteSearch, 100, false)
            {
                Id = "note_search_input", Width = 214, Height = 28, Margin = 0, Enabled = CanInteract,
                ForegroundColor = ControlColor, Tooltip = "Search personal and visible DM notes by title or content"
            }, EntryW, 40),
            new NuiSpacer { Height = 4, Margin = 0 },
            new NuiRow
            {
                Width = EntryW, Height = 32, Margin = 0, Padding = 0,
                Children =
                [
                    ImageControl("note_search", "ui_cdx_ent_n_v2", "Find", 109, 32, "Apply the search text"),
                    new NuiSpacer { Width = 8, Margin = 0 },
                    ImageControl("note_clear_search", "ui_cdx_ent_n_v2", "Clear", 109, 32, "Clear the search text")
                ]
            },
            new NuiSpacer { Height = 6, Margin = 0 }
        ];
        AddEntryRowsAndPagination(children, NotesPerPage);
        return EntryColumn(children);
    }

    public NuiColumn BuildEntryListInner()
    {
        List<NuiElement> children = [Heading("Entries", EntryW, 26)];
        AddEntryRowsAndPagination(children);
        return EntryColumn(children);
    }

    public NuiColumn BuildEconomyEntryList()
    {
        List<NuiElement> children =
        [
            new NuiLabel(IndustryName)
            { Height = 26, Margin = 0, HorizontalAlign = NuiHAlign.Center, ForegroundColor = Gold, Tooltip = IndustryName },
            new NuiLabel(ProficiencyLevelText)
            { Height = 26, Margin = 0, HorizontalAlign = NuiHAlign.Center, ForegroundColor = Gold },
            new NuiGroup
            {
                Width = EntryW, Height = 24, Margin = 0, Padding = 0, Border = false, Scrollbars = NuiScrollbars.None,
                Element = new NuiProgress(ProficiencyProgressValue)
                { Width = EntryW, Height = 24, Margin = 0, Visible = ShowProficiency, ForegroundColor = Gold }
            },
            new NuiLabel(ProficiencyProgressLabel)
            { Height = 20, Margin = 0, HorizontalAlign = NuiHAlign.Center, ForegroundColor = Gold },
            new NuiSpacer { Height = 6, Margin = 0 }
        ];
        AddEntryRowsAndPagination(children, rowHeight: 36);
        return EntryColumn(children);
    }

    public NuiColumn BuildTraitsEntryList()
    {
        List<NuiElement> children =
        [
            Heading("Entries", EntryW, 26),
            ImageControl("btn_select_traits", "ui_cdx_ent_n_v2", "Select Traits", EntryW, 32,
                "Open trait selection", "ui_cdx_i_head", 20),
            new NuiSpacer { Height = 6, Margin = 0 }
        ];
        AddEntryRowsAndPagination(children, rowHeight: 40);
        return EntryColumn(children);
    }

    private static NuiColumn EntryColumn(List<NuiElement> children) => new()
    { Width = EntryW, Height = 426, Margin = 0, Padding = 0, Children = children };

    private static NuiGroup InputFrame(NuiElement input, float width, float height) => new()
    {
        Width = width, Height = height, Margin = 0, Padding = 0, Border = false,
        Scrollbars = NuiScrollbars.None, Scissor = true,
        DrawList = [Picture("ui_cdx_bg", 0, 0, width, height), .. Frame(width, height, "pn", 12)],
        Element = Inset(input, width, height, 6)
    };

    private void AddEntryRowsAndPagination(List<NuiElement> children, int rowCount = EntriesPerPage, float rowHeight = 44)
    {
        for (int i = 0; i < rowCount; i++)
        {
            NuiImage entry = ImageControl($"btn_entry_{i}", EntryTextures[i], EntryNames[i],
                EntryW, rowHeight, EntryTooltips[i], visible: EntryRowVisible[i]);
            entry.DrawList =
            [
                new NuiDrawListText(ControlColor, new NuiRect(12, 4, 174, rowHeight / 2), EntryNames[i])
                { Order = NuiDrawListItemOrder.After },
                new NuiDrawListText(ControlColor, new NuiRect(12, rowHeight / 2, 174, rowHeight / 2 - 4), EntrySubtitles[i])
                { Order = NuiDrawListItemOrder.After },
                new NuiDrawListImage("ui_cdx_i_next", new NuiRect(190, (rowHeight - 24) / 2, 24, 24))
                { Color = ControlColor, Aspect = NuiAspect.Fit, Order = NuiDrawListItemOrder.After }
            ];
            children.Add(entry);
        }
        children.Add(new NuiSpacer { Margin = 0 });
        children.Add(new NuiRow
        {
            Width = EntryW, Height = 34, Margin = 0, Padding = 0,
            Children =
            [
                ImageControl("btn_prev_page", "ui_cdx_btn", null, 34, 34,
                    "Previous page", "ui_cdx_i_prev", 22, ShowPrevPage),
                new NuiSpacer { Margin = 0 },
                new NuiLabel(PageInfo)
                { Width = 100, Height = 34, Margin = 0, ForegroundColor = Gold,
                    HorizontalAlign = NuiHAlign.Center, VerticalAlign = NuiVAlign.Middle },
                new NuiSpacer { Margin = 0 },
                ImageControl("btn_next_page", "ui_cdx_btn", null, 34, 34,
                    "Next page", "ui_cdx_i_next", 22, ShowNextPage)
            ]
        });
    }

    private NuiImage ImageControl(string id, NuiProperty<string> texture, NuiProperty<string>? label,
        float width, float height, NuiProperty<string> tooltip, string? glyph = null, float glyphSize = 24,
        NuiProperty<bool>? visible = null)
    {
        ImageActionIds.Add(id);
        List<NuiDrawListItem> draw = new();
        if (glyph != null)
            draw.Add(new NuiDrawListImage(glyph, new NuiRect(12, (height - glyphSize) / 2, glyphSize, glyphSize))
            { Color = ControlColor, Aspect = NuiAspect.Fit, Order = NuiDrawListItemOrder.After });
        if (label != null)
        {
            float left = glyph == null ? 12 : 16 + glyphSize;
            draw.Add(new NuiDrawListText(ControlColor, new NuiRect(left, (height - 24) / 2, width - left - 12, 24), label)
            { Order = NuiDrawListItemOrder.After });
        }
        // Direction-only buttons center the glyph in the housing.
        if (label == null && glyph != null)
            draw[0] = new NuiDrawListImage(glyph, new NuiRect((width - glyphSize) / 2, (height - glyphSize) / 2, glyphSize, glyphSize))
            { Color = ControlColor, Aspect = NuiAspect.Fit, Order = NuiDrawListItemOrder.After };
        return new NuiImage(texture)
        {
            Id = id, Width = width, Height = height, Margin = 0, Padding = 0,
            ImageAspect = id == "codex_close" ? NuiAspect.Fit : NuiAspect.Stretch,
            Enabled = CanInteract, Visible = visible ?? new NuiValue<bool>(true),
            Tooltip = tooltip, DisabledTooltip = "Codex is busy; please wait", DrawList = draw
        };
    }

    private static string TabGlyph(CodexTab tab) => tab switch
    {
        CodexTab.Knowledge => "ui_cdx_i_book", CodexTab.Quests => "ui_cdx_i_shield",
        CodexTab.Notes => "ui_cdx_i_scroll", CodexTab.Reputation => "ui_cdx_i_people",
        CodexTab.Traits => "ui_cdx_i_head", CodexTab.Economy => "ui_cdx_i_coins",
        _ => "ui_cdx_i_book"
    };

    private static NuiLabel Heading(string text, float width, float height) => new(text)
    { Width = width, Height = height, Margin = 0, ForegroundColor = Gold, VerticalAlign = NuiVAlign.Middle };

    private static NuiGroup DarkPanel(float width, NuiElement content) => new()
    {
        Width = width, Height = BodyH, Margin = 0, Padding = 0, Border = false,
        Scrollbars = NuiScrollbars.None, Scissor = true,
        DrawList = [Picture("ui_cdx_bg", 0, 0, width, BodyH), .. Frame(width, BodyH, "pn", 18)],
        Element = Inset(content, width, BodyH, 12)
    };

    private static NuiColumn Inset(NuiElement content, float width, float height, float inset) => new()
    {
        Width = width, Height = height, Margin = 0, Padding = 0,
        Children =
        [
            new NuiSpacer { Height = inset, Margin = 0 },
            new NuiRow
            {
                Width = width, Height = height - 2 * inset, Margin = 0, Padding = 0,
                Children = [new NuiSpacer { Width = inset, Margin = 0 }, content, new NuiSpacer { Width = inset, Margin = 0 }]
            },
            new NuiSpacer { Height = inset, Margin = 0 }
        ]
    };

    private static List<NuiDrawListItem> Frame(float width, float height, string prefix, float corner) =>
    [
        Picture($"ui_cdx_{prefix}_tl", 0, 0, corner, corner),
        Picture($"ui_cdx_{prefix}_tr", width - corner, 0, corner, corner),
        Picture($"ui_cdx_{prefix}_bl", 0, height - corner, corner, corner),
        Picture($"ui_cdx_{prefix}_br", width - corner, height - corner, corner, corner),
        Picture($"ui_cdx_{prefix}_top", corner, 0, width - 2 * corner, corner),
        Picture($"ui_cdx_{prefix}_bot", corner, height - corner, width - 2 * corner, corner),
        Picture($"ui_cdx_{prefix}_left", 0, corner, corner, height - 2 * corner),
        Picture($"ui_cdx_{prefix}_right", width - corner, corner, corner, height - 2 * corner)
    ];

    private static NuiDrawListImage Picture(string resource, float x, float y, float width, float height) =>
        new(resource, new NuiRect(x, y, width, height))
        { Aspect = NuiAspect.Stretch, Order = NuiDrawListItemOrder.Before };
}
