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

    // --- Category sidebar group (swapped via SetGroupLayout per tab) ---
    public NuiGroup CategoryGroup = null!;

    // --- Entry list group (swapped via SetGroupLayout for Economy tab) ---
    public NuiGroup EntryListGroup = null!;

    // --- Detail pane binds (updated via SetBindValue — no SetGroupLayout needed) ---
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

                        // Detail pane (static layout with bound content)
                        BuildDetailPane()
                    }
                },

                // ── Bottom bar ──
                new NuiRow
                {
                    Height = 36f,
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer(),
                        new NuiButton("Close") { Id = "codex_close", Width = 90f, Height = 32f }
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
                new NuiButton("Knowledge") { Id = "tab_knowledge", Height = 35f },
                new NuiButton("Quests") { Id = "tab_quests", Height = 35f },
                new NuiButton("Notes") { Id = "tab_notes", Height = 35f },
                new NuiButton("Reputation") { Id = "tab_reputation", Height = 35f },
                new NuiButton("Traits") { Id = "tab_traits", Height = 35f },
                new NuiButton("Economy") { Id = "tab_economy", Height = 35f }
            }
        };
    }

    private NuiGroup BuildDetailPane()
    {
        return new NuiGroup
        {
            Width = 350f,
            Scrollbars = NuiScrollbars.Y,
            Border = true,
            Element = new NuiColumn
            {
                Children = new List<NuiElement>
                {
                    new NuiLabel(DetailTitle)
                    {
                        Height = 30f,
                        HorizontalAlign = NuiHAlign.Center,
                        VerticalAlign = NuiVAlign.Middle
                    },
                    new NuiSpacer { Height = 4f },
                    new NuiText(DetailBody)
                    {
                        Scrollbars = NuiScrollbars.None
                    }
                }
            }
        };
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
    private void AddEntryRowsAndPagination(List<NuiElement> children)
    {
        // 8 entry rows
        for (int i = 0; i < EntriesPerPage; i++)
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
                    Width = 40f,
                    Height = 30f,
                    Visible = ShowNextPage,
                    Tooltip = "Next page"
                }
            }
        });
    }
}
