using AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui;
using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Nui;

/// <summary>
///     Three-pane trait selection view: category sidebar | paginated trait list | detail + actions.
/// </summary>
public sealed class TraitSelectionView : ScryView<TraitSelectionPresenter>, IToolWindow
{
    public const float WindowW = 820f;
    public const float WindowH = 620f;
    public const int EntriesPerPage = 8;

    // Category sidebar (swapped via SetGroupLayout)
    public NuiGroup CategoryGroup = null!;

    // Detail pane binds
    public readonly NuiBind<string> DetailTitle = new("trait_detail_title");
    public readonly NuiBind<string> DetailBody = new("trait_detail_body");
    public readonly NuiBind<bool> ShowSelectButton = new("trait_show_select");
    public readonly NuiBind<bool> ShowDeselectButton = new("trait_show_deselect");
    public readonly NuiBind<string> BudgetLabel = new("trait_budget");

    // Pagination binds
    public readonly NuiBind<string> PageInfo = new("trait_page_info");
    public readonly NuiBind<bool> ShowPrevPage = new("trait_show_prev");
    public readonly NuiBind<bool> ShowNextPage = new("trait_show_next");

    // Per-row binds
    public readonly List<NuiBind<string>> EntryNames = [];
    public readonly List<NuiBind<string>> EntrySubtitles = [];
    public readonly List<NuiBind<bool>> EntryRowVisible = [];

    public TraitSelectionView(NwPlayer player)
    {
        for (int i = 0; i < EntriesPerPage; i++)
        {
            EntryNames.Add(new NuiBind<string>($"trait_name_{i}"));
            EntrySubtitles.Add(new NuiBind<string>($"trait_sub_{i}"));
            EntryRowVisible.Add(new NuiBind<bool>($"trait_vis_{i}"));
        }

        Presenter = new TraitSelectionPresenter(this, player);

        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override TraitSelectionPresenter Presenter { get; protected set; }

    // IToolWindow
    public string Id => "playertools.traitselection";
    public bool ListInPlayerTools => true;
    public bool RequiresPersistedCharacter => true;
    public string Title => "Traits";
    public string CategoryTag => "Character";

    public IScryPresenter ForPlayer(NwPlayer player)
    {
        return new TraitSelectionView(player).Presenter;
    }

    public override NuiLayout RootLayout()
    {
        const float bodyH = WindowH - 90f;

        return new NuiColumn
        {
            Children = new List<NuiElement>
            {
                // Budget bar
                new NuiRow
                {
                    Height = 40f,
                    Children = new List<NuiElement>
                    {
                        new NuiLabel(BudgetLabel)
                        {
                            Height = 35f,
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle
                        },
                        new NuiSpacer(),
                        new NuiButton("Confirm")
                        {
                            Id = "btn_confirm",
                            Width = 100f,
                            Height = 35f,
                            Tooltip = "Confirm unconfirmed trait selections"
                        }
                    }
                },

                // Main body: sidebar | trait list | detail pane
                new NuiRow
                {
                    Height = bodyH,
                    Children = new List<NuiElement>
                    {
                        // Category sidebar
                        new NuiGroup
                        {
                            Id = "grp_trait_categories",
                            Element = new NuiColumn
                            {
                                Children = new List<NuiElement> { new NuiSpacer() }
                            },
                            Width = 130f,
                            Scrollbars = NuiScrollbars.None,
                            Border = true
                        }.Assign(out CategoryGroup),

                        // Trait list
                        new NuiGroup
                        {
                            Element = BuildEntryList(),
                            Width = 270f,
                            Scrollbars = NuiScrollbars.None,
                            Border = true
                        },

                        // Detail pane with select/deselect actions
                        BuildDetailPane()
                    }
                },

                // Bottom bar
                new NuiRow
                {
                    Height = 36f,
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer(),
                        new NuiButton("Close")
                        {
                            Id = "btn_close",
                            Width = 90f,
                            Height = 32f
                        }
                    }
                }
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
                    },
                    new NuiSpacer { Height = 8f },
                    new NuiRow
                    {
                        Height = 36f,
                        Children = new List<NuiElement>
                        {
                            new NuiSpacer(),
                            new NuiButton("Select")
                            {
                                Id = "btn_select_trait",
                                Width = 100f,
                                Height = 32f,
                                Visible = ShowSelectButton,
                                Tooltip = "Add this trait to your character"
                            },
                            new NuiButton("Remove")
                            {
                                Id = "btn_deselect_trait",
                                Width = 100f,
                                Height = 32f,
                                Visible = ShowDeselectButton,
                                Tooltip = "Remove this unconfirmed trait"
                            },
                            new NuiSpacer()
                        }
                    }
                }
            }
        };
    }

    private NuiColumn BuildEntryList()
    {
        List<NuiElement> children = [];

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
                        Id = $"btn_trait_{i}",
                        Width = 32f,
                        Height = 32f,
                        Tooltip = "View details"
                    }
                }
            });
        }

        // Pagination
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

        return new NuiColumn { Children = children };
    }
}
