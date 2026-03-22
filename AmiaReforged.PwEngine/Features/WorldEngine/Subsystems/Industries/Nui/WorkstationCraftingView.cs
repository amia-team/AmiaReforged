using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Nui;

/// <summary>
/// NUI view for the workstation crafting UI.
/// Two-pane layout: searchable/paginated recipe list on the left, detail pane on the right.
/// </summary>
public sealed class WorkstationCraftingView : ScryView<WorkstationCraftingPresenter>
{
    public const float WindowW = 700f;
    public const float WindowH = 560f;
    public const int EntriesPerPage = 8;

    // --- Search ---
    public readonly NuiBind<string> SearchText = new("wc_search");

    // --- Entry list ---
    public readonly NuiBind<string> PageInfo = new("wc_page_info");
    public readonly NuiBind<bool> ShowPrevPage = new("wc_show_prev");
    public readonly NuiBind<bool> ShowNextPage = new("wc_show_next");

    public readonly List<NuiBind<string>> EntryNames = [];
    public readonly List<NuiBind<string>> EntrySubtitles = [];
    public readonly List<NuiBind<bool>> EntryRowVisible = [];

    // --- Detail pane ---
    public readonly NuiBind<string> DetailTitle = new("wc_detail_title");
    public readonly NuiBind<string> DetailBody = new("wc_detail_body");
    public readonly NuiBind<bool> ShowCraftButton = new("wc_show_craft");

    // --- Detail pane group (swapped via SetGroupLayout when recipe selected) ---
    public NuiGroup DetailGroup = null!;

    // --- Per-ingredient quality combo binds (pre-allocated for up to MaxIngredientSlots) ---
    public const int MaxIngredientSlots = 6;
    public readonly List<NuiBind<List<NuiComboEntry>>> IngredientQualityOptions = [];
    public readonly List<NuiBind<int>> IngredientQualitySelected = [];

    public WorkstationCraftingView(NwPlayer player, WorkstationTag workstationTag, string workstationName)
    {
        for (int i = 0; i < EntriesPerPage; i++)
        {
            EntryNames.Add(new NuiBind<string>($"wc_name_{i}"));
            EntrySubtitles.Add(new NuiBind<string>($"wc_sub_{i}"));
            EntryRowVisible.Add(new NuiBind<bool>($"wc_vis_{i}"));
        }

        for (int i = 0; i < MaxIngredientSlots; i++)
        {
            IngredientQualityOptions.Add(new NuiBind<List<NuiComboEntry>>($"wc_ing_opts_{i}"));
            IngredientQualitySelected.Add(new NuiBind<int>($"wc_ing_sel_{i}"));
        }

        Presenter = new WorkstationCraftingPresenter(this, player, workstationTag, workstationName);
        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override WorkstationCraftingPresenter Presenter { get; protected set; }

    public override NuiLayout RootLayout()
    {
        const float bodyH = WindowH - 80f;

        return new NuiColumn
        {
            Children =
            [
                // Main body: recipe list | detail pane
                new NuiRow
                {
                    Height = bodyH,
                    Children =
                    [
                        // Left pane — search + list + pagination
                        new NuiGroup
                        {
                            Element = BuildRecipeListPane(),
                            Width = 300f,
                            Scrollbars = NuiScrollbars.None,
                            Border = true
                        },
                        // Right pane — detail (swapped via SetGroupLayout per recipe)
                        BuildDetailPane()
                            .Assign(out DetailGroup)
                    ]
                },
                // Bottom bar
                new NuiRow
                {
                    Height = 36f,
                    Children =
                    [
                        new NuiSpacer(),
                        new NuiButton("Close")
                        {
                            Id = "btn_close",
                            Width = 90f,
                            Height = 32f
                        }
                    ]
                }
            ]
        };
    }

    private NuiColumn BuildRecipeListPane()
    {
        List<NuiElement> children =
        [
            // Search bar
            new NuiRow
            {
                Height = 32f,
                Children =
                [
                    new NuiTextEdit("Search...", SearchText, 64, false)
                    {
                        Width = 240f,
                        Height = 28f
                    },
                    new NuiButtonImage("ir_abort")
                    {
                        Id = "btn_clear_search",
                        Aspect = 1f,
                        Width = 28f,
                        Height = 28f,
                        Tooltip = "Clear search"
                    }
                ]
            },
            new NuiSpacer { Height = 4f }
        ];

        // Recipe rows
        for (int i = 0; i < EntriesPerPage; i++)
        {
            children.Add(new NuiRow
            {
                Height = 48f,
                Visible = EntryRowVisible[i],
                Children =
                [
                    new NuiColumn
                    {
                        Children =
                        [
                            new NuiLabel(EntryNames[i])
                            {
                                Height = 26f,
                                HorizontalAlign = NuiHAlign.Left,
                                VerticalAlign = NuiVAlign.Bottom
                            },
                            new NuiLabel(EntrySubtitles[i])
                            {
                                Height = 18f,
                                HorizontalAlign = NuiHAlign.Left,
                                VerticalAlign = NuiVAlign.Top,
                                ForegroundColor = new Color(160, 140, 100)
                            }
                        ]
                    },
                    new NuiButton(">")
                    {
                        Id = $"btn_recipe_{i}",
                        Width = 32f,
                        Height = 32f
                    }
                ]
            });
        }

        // Pagination
        children.Add(new NuiSpacer());
        children.Add(new NuiRow
        {
            Height = 35f,
            Children =
            [
                new NuiButton("<")
                {
                    Id = "btn_prev_page",
                    Width = 40f,
                    Height = 30f,
                    Visible = ShowPrevPage
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
                    Visible = ShowNextPage
                }
            ]
        });

        return new NuiColumn { Children = children };
    }

    private NuiGroup BuildDetailPane()
    {
        return new NuiGroup
        {
            Id = "grp_detail",
            Width = 360f,
            Scrollbars = NuiScrollbars.Y,
            Border = true,
            Element = BuildDefaultDetailColumn()
        };
    }

    /// <summary>
    /// Default detail pane content: title, body text, and craft button.
    /// Used when no recipe is selected or when resetting.
    /// </summary>
    public NuiColumn BuildDefaultDetailColumn()
    {
        return new NuiColumn
        {
            Children =
            [
                new NuiLabel(DetailTitle)
                {
                    Height = 30f,
                    HorizontalAlign = NuiHAlign.Center,
                    VerticalAlign = NuiVAlign.Middle
                },
                new NuiSpacer { Height = 4f },
                new NuiText(DetailBody) { Scrollbars = NuiScrollbars.None },
                new NuiSpacer { Height = 8f },
                new NuiRow
                {
                    Height = 36f,
                    Children =
                    [
                        new NuiSpacer(),
                        new NuiButton("Craft")
                        {
                            Id = "btn_craft",
                            Width = 100f,
                            Height = 32f,
                            Visible = ShowCraftButton
                        },
                        new NuiSpacer()
                    ]
                }
            ]
        };
    }

    /// <summary>
    /// Builds the detail pane layout for a selected recipe with per-ingredient quality dropdowns.
    /// </summary>
    /// <param name="recipe">The selected recipe.</param>
    /// <param name="ingredientSlots">Per-ingredient quality info: list of (tag, name, quantity, availableQualities).
    /// Each availableQualities is a sorted list of quality tiers found in inventory.</param>
    public NuiColumn BuildRecipeDetailLayout(Recipe recipe,
        List<(string Tag, string Name, int Qty, List<int> AvailableQualities)> ingredientSlots)
    {
        List<NuiElement> children =
        [
            // Title
            new NuiLabel(DetailTitle)
            {
                Height = 30f,
                HorizontalAlign = NuiHAlign.Center,
                VerticalAlign = NuiVAlign.Middle
            },
            new NuiSpacer { Height = 4f },
            // Body text (description, products, modifiers — everything except ingredients)
            new NuiText(DetailBody) { Scrollbars = NuiScrollbars.None },
            new NuiSpacer { Height = 6f },
            // Ingredients header
            new NuiLabel("--- Ingredients ---")
            {
                Height = 22f,
                HorizontalAlign = NuiHAlign.Left,
                VerticalAlign = NuiVAlign.Middle
            }
        ];

        // Per-ingredient rows with quality selection
        for (int i = 0; i < ingredientSlots.Count && i < MaxIngredientSlots; i++)
        {
            (string tag, string name, int qty, List<int> qualities) = ingredientSlots[i];
            Ingredient ingredient = recipe.Ingredients[i];
            string consumed = ingredient.IsConsumed ? "" : " (catalyst)";

            if (qualities.Count <= 1)
            {
                // Static text — one or zero qualities available
                string qualLabel = qualities.Count == 1
                    ? CraftingQuality.Label(qualities[0])
                    : "None";
                children.Add(new NuiRow
                {
                    Height = 28f,
                    Children =
                    [
                        new NuiLabel($"  {qty}x {name} — {qualLabel}{consumed}")
                        {
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle
                        }
                    ]
                });
            }
            else
            {
                // Combo row — multiple quality tiers available
                children.Add(new NuiRow
                {
                    Height = 32f,
                    Children =
                    [
                        new NuiLabel($"  {qty}x {name}{consumed}")
                        {
                            Width = 160f,
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle
                        },
                        new NuiCombo
                        {
                            Id = $"wc_ing_sel_{i}",
                            Entries = IngredientQualityOptions[i],
                            Selected = IngredientQualitySelected[i],
                            Width = 160f,
                            Height = 28f
                        }
                    ]
                });
            }
        }

        // Spacer + Craft button
        children.Add(new NuiSpacer { Height = 8f });
        children.Add(new NuiRow
        {
            Height = 36f,
            Children =
            [
                new NuiSpacer(),
                new NuiButton("Craft")
                {
                    Id = "btn_craft",
                    Width = 100f,
                    Height = 32f,
                    Visible = ShowCraftButton
                },
                new NuiSpacer()
            ]
        });

        return new NuiColumn { Children = children };
    }
}
