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

    // --- Detail pane group (swapped via SetGroupLayout on entry selection) ---
    public NuiGroup DetailGroup = null!;

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
        return new NuiColumn
        {
            Children = new List<NuiElement>
            {
                // ── Tab bar ──
                BuildTabBar(),

                // ── Main body: sidebar | entry list | detail pane ──
                new NuiRow
                {
                    Height = WindowH - 90f,
                    Children = new List<NuiElement>
                    {
                        // Category sidebar
                        new NuiGroup
                        {
                            Id = "grp_categories",
                            Width = 140f,
                            Height = WindowH - 90f,
                            Scrollbars = NuiScrollbars.None,
                            Border = true
                        }.Assign(out CategoryGroup),

                        // Entry list column
                        BuildEntryListColumn(),

                        // Detail pane
                        new NuiGroup
                        {
                            Id = "grp_detail",
                            Width = 370f,
                            Height = WindowH - 90f,
                            Scrollbars = NuiScrollbars.Y,
                            Border = true
                        }.Assign(out DetailGroup)
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
                new NuiButton("Reputation") { Id = "tab_reputation", Height = 35f }
            }
        };
    }

    private NuiColumn BuildEntryListColumn()
    {
        List<NuiElement> children = new();

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
                        Width = 210f,
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
                new NuiSpacer { Width = 10f },
                new NuiLabel(PageInfo)
                {
                    Width = 100f,
                    HorizontalAlign = NuiHAlign.Center,
                    VerticalAlign = NuiVAlign.Middle
                },
                new NuiSpacer { Width = 10f },
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

        return new NuiColumn
        {
            Width = 280f,
            Height = WindowH - 90f,
            Children = children
        };
    }
}
