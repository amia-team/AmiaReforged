using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Nui.Player;

/// <summary>
/// Player-facing Codex view with three-panel layout:
/// category sidebar, paginated entry list, and scrollable detail pane.
/// Uses manual rows (not NuiList) so entries can be individually styled.
/// </summary>
public sealed class PlayerCodexView : ScryView<PlayerCodexPresenter>
{
    public const float WindowW = 820f;
    public const float WindowH = 620f;

    public const int EntriesPerPage = 8;
    public const int NotesPerPage = 6;
    public NuiGroup DetailGroup = null!;
    public readonly NuiBind<string> NoteTitle = new("codex_note_title");
    public readonly NuiBind<string> NoteContent = new("codex_note_content");
    public readonly NuiBind<int> NoteCategorySelection = new("codex_note_category");
    public readonly NuiBind<string> NoteSearch = new("codex_note_search");
    public readonly NuiBind<string> Status = new("codex_status");
    public readonly NuiBind<bool> CanInteract = new("codex_can_interact");
    public readonly NuiBind<bool> CanCloseWindow = new("codex_can_close");
    public readonly NuiBind<bool> ShowNoteActions = new("codex_show_note_actions");
    public readonly NuiBind<bool> ShowConfirmation = new("codex_show_confirmation");
    public readonly NuiBind<string> ConfirmLabel = new("codex_confirm_label");

    // --- Category sidebar group (swapped via SetGroupLayout per tab) ---
    public NuiGroup CategoryGroup = null!;

    // --- Entry list group (swapped via SetGroupLayout for Economy tab) ---
    public NuiGroup EntryListGroup = null!;

    // --- Detail pane binds ---
    public readonly NuiBind<string> DetailTitle = new("codex_detail_title");
    public readonly NuiBind<string> DetailBody = new("codex_detail_body");

    // --- Economy tab proficiency binds ---
    public readonly NuiBind<string> ProficiencyLevelText = new("codex_prof_level");
    public readonly NuiBind<float> ProficiencyProgressValue = new("codex_prof_progress");
    public readonly NuiBind<string> ProficiencyProgressLabel = new("codex_prof_label");

    // --- Pagination binds ---
    public readonly NuiBind<string> PageInfo = new("codex_page_info");
    public readonly NuiBind<bool> ShowPrevPage = new("codex_show_prev");
    public readonly NuiBind<bool> ShowNextPage = new("codex_show_next");

    // --- Select Traits action (Traits tab only) ---
    public readonly NuiBind<bool> ShowSelectTraits = new("codex_show_select_traits");

    // --- Per-row binds (8 rows) ---
    public readonly List<NuiBind<string>> EntryNames = new();
    public readonly List<NuiBind<string>> EntrySubtitles = new();
    public readonly List<NuiBind<bool>> EntryRowVisible = new();

    public PlayerCodexView(NwPlayer player)
    {
        // Initialize per-row binds
        for (int i = 0; i < EntriesPerPage; i++)
        {
            EntryNames.Add(new NuiBind<string>($"entry_name_{i}"));
            EntrySubtitles.Add(new NuiBind<string>($"entry_sub_{i}"));
            EntryRowVisible.Add(new NuiBind<bool>($"entry_vis_{i}"));
        }

        Presenter = new PlayerCodexPresenter(this, player);

        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override PlayerCodexPresenter Presenter { get; protected set; }

    public override NuiLayout RootLayout()
    {
        // Available height for the main body area
        const float bodyH = WindowH - 90f;

        return new NuiColumn
        {
            Children = new List<NuiElement>
            {
                // ── Tab bar ──
                BuildTabBar(),

                // ── Main body: sidebar | entry list | detail pane ──
                new NuiRow
                {
                    Height = bodyH,
                    Children = new List<NuiElement>
                    {
                        // Category sidebar (swapped via SetGroupLayout)
                        new NuiGroup
                        {
                            Id = "grp_categories",
                            Element = new NuiColumn { Children = new List<NuiElement> { new NuiSpacer() } },
                            Width = 130f,
                            Scrollbars = NuiScrollbars.None,
                            Border = true
                        }.Assign(out CategoryGroup),

                        // Entry list (swapped via SetGroupLayout for Economy tab)
                        new NuiGroup
                        {
                            Id = "grp_entry_list",
                            Element = BuildEntryListInner(),
                            Width = 270f,
                            Scrollbars = NuiScrollbars.None,
                            Border = true
                        }.Assign(out EntryListGroup),

                        // Detail pane (swapped for note editing)
                        BuildDetailPane()
                    }
                },

                // ── Bottom bar ──
                new NuiRow
                {
                    Height = 36f,
                    Children = new List<NuiElement>
                    {
                        new NuiLabel(Status) { Width = 430f, Height = 32f, VerticalAlign = NuiVAlign.Middle },
                        new NuiButton(ConfirmLabel)
                        {
                            Id = "codex_confirm", Width = 90f, Height = 32f,
                            Visible = ShowConfirmation, Enabled = CanInteract
                        },
                        new NuiButton("Keep")
                        {
                            Id = "codex_keep", Width = 70f, Height = 32f,
                            Visible = ShowConfirmation, Enabled = CanInteract
                        },
                        new NuiSpacer(),
                        new NuiButton("Close") { Id = "codex_close", Width = 90f, Height = 32f, Enabled = CanInteract }
                    }
                }
            }
        };
    }

    private NuiRow BuildTabBar()
    {
        return new NuiRow
        {
            Height = 40f,
            Children = new List<NuiElement>
            {
                new NuiButton("Knowledge") { Id = "tab_knowledge", Enabled = CanInteract, Height = 35f },
                new NuiButton("Quests") { Id = "tab_quests", Enabled = CanInteract, Height = 35f },
                new NuiButton("Notes") { Id = "tab_notes", Enabled = CanInteract, Height = 35f },
                new NuiButton("Reputation") { Id = "tab_reputation", Enabled = CanInteract, Height = 35f },
                new NuiButton("Traits") { Id = "tab_traits", Enabled = CanInteract, Height = 35f },
                new NuiButton("Economy") { Id = "tab_economy", Enabled = CanInteract, Height = 35f },

                new NuiButton("Select Traits") { Id = "btn_select_traits", Enabled = CanInteract, Height = 35f, Visible = ShowSelectTraits }
            }
        };
    }

    private NuiGroup BuildDetailPane() => new NuiGroup
    {
        Id = "grp_detail",
        Width = 350f,
        Scrollbars = NuiScrollbars.Y,
        Border = true,
        Element = BuildDetailContent()
    }.Assign(out DetailGroup);

    public NuiColumn BuildDetailContent() => new()
    {
        Children =
        [
            new NuiLabel(DetailTitle)
            {
                Height = 30f, HorizontalAlign = NuiHAlign.Center, VerticalAlign = NuiVAlign.Middle
            },
            new NuiRow
            {
                Height = 34f, Visible = ShowNoteActions,
                Children =
                [
                    new NuiButton("Edit") { Id = "note_edit", Height = 30f, Enabled = CanInteract },
                    new NuiButton("Delete") { Id = "note_delete", Height = 30f, Enabled = CanInteract }
                ]
            },
            new NuiSpacer { Height = 4f },
            new NuiText(DetailBody)
        ]
    };

    public NuiColumn BuildNoteEditor() => new()
    {
        Children =
        [
            new NuiLabel("Edit Note") { Height = 28f, HorizontalAlign = NuiHAlign.Center },
            new NuiTextEdit("Title (optional)", NoteTitle, CodexNoteEntry.MaxTitleLength, false)
            {
                Height = 32f, Enabled = CanInteract
            },
            new NuiCombo
            {
                Id = "note_category", Height = 32f, Enabled = CanInteract,
                Selected = NoteCategorySelection,
                Entries = new List<NuiComboEntry>
                {
                    new("General", (int)NoteCategory.General),
                    new("Quest", (int)NoteCategory.Quest),
                    new("Character", (int)NoteCategory.Character),
                    new("Location", (int)NoteCategory.Location)
                }
            },
            new NuiTextEdit("Note", NoteContent, CodexNoteEntry.MaxPlayerContentLength, true)
            {
                Height = 335f, WordWrap = true, Enabled = CanInteract
            },
            new NuiRow
            {
                Height = 34f,
                Children =
                [
                    new NuiButton("Save") { Id = "note_save", Height = 30f, Enabled = CanInteract },
                    new NuiButton("Cancel") { Id = "note_cancel", Height = 30f, Enabled = CanInteract }
                ]
            }
        ]
    };

    public NuiColumn BuildNotesEntryList()
    {
        List<NuiElement> children =
        [
            new NuiButton("New Note") { Id = "note_new", Height = 32f, Enabled = CanInteract },
            new NuiRow
            {
                Height = 32f,
                Children =
                [
                    new NuiTextEdit("Search notes", NoteSearch, 100, false) { Height = 28f, Enabled = CanInteract },
                    new NuiButton("Find") { Id = "note_search", Width = 48f, Height = 28f, Enabled = CanInteract },
                    new NuiButton("X")
                    {
                        Id = "note_clear_search", Width = 28f, Height = 28f,
                        Tooltip = "Clear search", Enabled = CanInteract
                    }
                ]
            }
        ];
        AddEntryRowsAndPagination(children, NotesPerPage);
        return new NuiColumn { Children = children };
    }

    /// <summary>
    /// Standard entry list layout used by all tabs except Economy.
    /// </summary>
    public NuiColumn BuildEntryListInner()
    {
        List<NuiElement> children = new();
        AddEntryRowsAndPagination(children);
        return new NuiColumn { Children = children };
    }

    /// <summary>
    /// Economy tab middle pane: proficiency info header + paginated knowledge entries.
    /// </summary>
    public NuiColumn BuildEconomyEntryList()
    {
        List<NuiElement> children = new()
        {
            // Proficiency level label (centered)
            new NuiRow
            {
                Height = 30f,
                Children = new List<NuiElement>
                {
                    new NuiSpacer(),
                    new NuiLabel(ProficiencyLevelText)
                    {
                        HorizontalAlign = NuiHAlign.Center,
                        VerticalAlign = NuiVAlign.Middle
                    },
                    new NuiSpacer()
                }
            },
            // Progress bar
            new NuiRow
            {
                Height = 28f,
                Children = new List<NuiElement>
                {
                    new NuiSpacer { Width = 10f },
                    new NuiProgress(ProficiencyProgressValue) { Height = 24f },
                    new NuiSpacer { Width = 10f }
                }
            },
            // XP label underneath progress bar
            new NuiRow
            {
                Height = 22f,
                Children = new List<NuiElement>
                {
                    new NuiSpacer(),
                    new NuiLabel(ProficiencyProgressLabel)
                    {
                        HorizontalAlign = NuiHAlign.Center,
                        VerticalAlign = NuiVAlign.Middle,
                        ForegroundColor = new Color(160, 140, 100)
                    },
                    new NuiSpacer()
                }
            },
            new NuiSpacer { Height = 6f }
        };

        AddEntryRowsAndPagination(children);
        return new NuiColumn { Children = children };
    }

    /// <summary>
    /// Shared helper: appends 8 entry rows + pagination controls to the given children list.
    /// </summary>
    private void AddEntryRowsAndPagination(List<NuiElement> children, int rowCount = EntriesPerPage)
    {
        // 8 entry rows
        for (int i = 0; i < rowCount; i++)
        {
            children.Add(new NuiRow
            {
                Height = 52f,
                Visible = EntryRowVisible[i],
                Children = new List<NuiElement>
                {
                    new NuiColumn
                    {
                        Children = new List<NuiElement>
                        {
                            new NuiLabel(EntryNames[i])
                            {
                                Height = 28f,
                                HorizontalAlign = NuiHAlign.Left,
                                VerticalAlign = NuiVAlign.Bottom
                            },
                            new NuiLabel(EntrySubtitles[i])
                            {
                                Height = 20f,
                                HorizontalAlign = NuiHAlign.Left,
                                VerticalAlign = NuiVAlign.Top,
                                ForegroundColor = new Color(160, 140, 100)
                            }
                        }
                    },
                    new NuiButton(">")
                    {
                        Id = $"btn_entry_{i}",
                        Enabled = CanInteract,
                        Width = 32f,
                        Height = 32f,
                        Tooltip = "View details"
                    }
                }
            });
        }

        // Pagination row
        children.Add(new NuiRow
        {
            Height = 35f,
            Children = new List<NuiElement>
            {
                new NuiButton("<")
                {
                    Id = "btn_prev_page",
                    Enabled = CanInteract,
                    Width = 40f,
                    Height = 30f,
                    Visible = ShowPrevPage,
                    Tooltip = "Previous page"
                },
                new NuiSpacer(),
                new NuiLabel(PageInfo)
                {
                    Width = 80f,
                    HorizontalAlign = NuiHAlign.Center,
                    VerticalAlign = NuiVAlign.Middle
                },
                new NuiSpacer(),
                new NuiButton(">")
                {
                    Id = "btn_next_page",
                    Enabled = CanInteract,
                    Width = 40f,
                    Height = 30f,
                    Visible = ShowNextPage,
                    Tooltip = "Next page"
                }
            }
        });
    }
}
