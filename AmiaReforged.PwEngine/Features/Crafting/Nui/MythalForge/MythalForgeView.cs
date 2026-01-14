using AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge.SubViews.ActiveProperties;
using AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge.SubViews.ChangeList;
using AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge.SubViews.MythalCategory;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge;

/// <summary>
///     Represents the view for the Mythal Forge crafting system.
/// </summary>
public sealed class MythalForgeView : ScryView<MythalForgePresenter>
{
    public const string ApplyNameButtonId = "apply_name";
    public const string ApplyChanges = "apply_changes";
    public const string? Cancel = "cancel";
    public const string ToCasterWeapon = "to_caster_weapon";

    public readonly ActivePropertiesView ActivePropertiesView;


    /// <summary>
    ///     Gets the category view for the Mythal Forge. Public so that the presenter can access it.
    /// </summary>
    public readonly MythalCategoryView CategoryView;

    public readonly ChangelistView ChangelistView;


    /// <summary>
    ///     Initializes a new instance of the <see cref="MythalForgeView" /> class. Initializes the presenter and sub-views.
    /// </summary>
    /// <param name="propertyData">The crafting property data.</param>
    /// <param name="budget">The crafting budget service.</param>
    /// <param name="item">The item being crafted.</param>
    /// <param name="player">The player performing the crafting.</param>
    /// <param name="validator"></param>
    /// <param name="dcCalculator"></param>
    public MythalForgeView(CraftingPropertyData propertyData, CraftingBudgetService budget, NwItem item,
        NwPlayer player, PropertyValidator validator, DifficultyClassCalculator dcCalculator)
    {
        Presenter = new MythalForgePresenter(this, propertyData, budget, item, player, validator, dcCalculator);

        CategoryView = new MythalCategoryView(Presenter);
        ActivePropertiesView = new ActivePropertiesView();
        ChangelistView = new ChangelistView(Presenter);

        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    /// <summary>
    ///     Gets the presenter associated with this view.
    /// </summary>
    public override MythalForgePresenter Presenter { get; protected set; }

    /// <summary>
    ///     Gets the binding for the item name.
    /// </summary>
    public NuiBind<string> ItemName { get; } = new(key: "item_name");

    public NuiBind<string> MaxPowers { get; } = new(key: "max_powers");
    public NuiBind<string> RemainingPowers { get; } = new(key: "remaining_powers");
    public NuiBind<bool> ApplyEnabled { get; } = new(key: "apply_enabled");

    public NuiBind<bool> EncourageDifficulty { get; } = new(key: "encourage_difficulty");
    public NuiBind<bool> EncourageGold { get; } = new(key: "encourage_gold");

    public NuiBind<string> GoldCost { get; } = new(key: "gold_cost");
    public NuiBind<Color> GoldCostColor { get; } = new(key: "gold_cost_color");
    public NuiBind<string> GoldCostTooltip { get; } = new(key: "gold_cost_tooltip");


    public NuiBind<string> DifficultyClass { get; } = new(key: "difficulty_class");

    public NuiBind<Color> SkillColor { get; } = new(key: "dc_color");
    public NuiBind<string> SkillTooltip { get; } = new(key: "skill_name");
    public NuiBind<bool> ToCasterWeaponEnabled { get; } = new(key: "to_caster_enabled");

    /// <summary>
    ///     Defines the root layout of the Mythal Forge view. The layout is composed of several sub-views, which are
    ///     created and referenced by the view in the constructor.
    /// </summary>
    /// <returns>The root layout of the view.</returns>
    public override NuiLayout RootLayout() =>
        new NuiColumn
        {
            Children =
            {
                new NuiRow
                {
                    Width = 0f,
                    Height = 0f,
                    Children = new List<NuiElement>(),
                    DrawList = [new NuiDrawListImage("ui_forge", new NuiRect(-5f, -25f, 1220f, 813f))]
                },
                new NuiSpacer { Height = 170f },
                new NuiRow
                {
                    Children =
                    {
                        new NuiTextEdit(label: "Edit Name", ItemName, 100, false)
                        {
                            Width = 200f,
                            Height = 38f
                        },
                        new NuiButtonImage("ui_btn_rename")
                        {
                            Id = ApplyNameButtonId,
                            Width = 150f,
                            Height = 38f
                        }
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiTextEdit(label: "Filter...", CategoryView.PropertyFilterText, 100, false)
                        {
                            Width = 100f,
                            Height = 35f
                        },
                        new NuiSpacer { Width = 5f },
                        new NuiCombo
                        {
                            Id = MythalCategoryView.CategoryFilterChanged,
                            Width = 120f,
                            Height = 35f,
                            Entries = CategoryView.CategoryFilterOptions,
                            Selected = CategoryView.CategoryFilterIndex
                        },
                        new NuiSpacer { Width = 10f },
                        new NuiButtonImage("isk_search")
                        {
                            Id = MythalCategoryView.SearchPropertiesButton,
                            Width = 35f,
                            Height = 35f,
                            Tooltip = "Search/filter properties"
                        },
                        new NuiSpacer { Width = 20f },
                        new NuiLabel(label: "Max Powers:")
                        {
                            VerticalAlign = NuiVAlign.Middle,
                            Width = 85f,
                            Height = 35f
                        },
                        new NuiSpacer { Width = 10f },
                        new NuiLabel(MaxPowers)
                        {
                            VerticalAlign = NuiVAlign.Middle,
                            Width = 50f,
                            Height = 35f
                        },
                        new NuiSpacer { Width = 230f },
                        new NuiLabel(label: "Remaining Powers:")
                        {
                            VerticalAlign = NuiVAlign.Middle,
                            Width = 125f,
                            Height = 35f
                        },
                        new NuiSpacer { Width = 10f },
                        new NuiLabel(RemainingPowers)
                        {
                            VerticalAlign = NuiVAlign.Middle,
                            Width = 50f,
                            Height = 35f
                        }
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        CategoryView.RootLayout(),
                        new NuiSpacer { Width = 50f },
                        new NuiGroup
                        {
                            Element = ActivePropertiesView.RootLayout(),
                            Border = true,
                            Width = 430f,
                            Height = 420f
                        },
                        new NuiSpacer { Width = 50f },
                        new NuiGroup
                        {
                            Element = ChangelistView.RootLayout(),
                            Border = true,
                            Width = 430f,
                            Height = 420f
                        }
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiButtonImage("ui_btn_cancelf")
                        {
                            Id = Cancel,
                            Width = 150f,
                            Height = 38f
                        },
                        new NuiSpacer { Width = 20f },
                        new NuiButtonImage("ui_btn_forge")
                        {
                            Id = ApplyChanges,
                            Width = 150f,
                            Height = 38f,
                            Tooltip = "Forge your item!",
                            Enabled = ApplyEnabled
                        },
                        new NuiSpacer { Width = 20f },
                        new NuiButtonImage("ui_btn_castweap")
                        {
                            Id = ToCasterWeapon,
                            Width = 150f,
                            Height = 38f,
                            Enabled = ToCasterWeaponEnabled,
                            Tooltip = "Convert to a caster weapon. Incompatible properties will be removed."
                        },
                        new NuiSpacer { Width = 20f },
                        new NuiLabel(label: "Difficulty:")
                        {
                            Width = 70f,
                            Height = 38f,
                            VerticalAlign = NuiVAlign.Middle
                        },
                        new NuiSpacer { Width = 20f },
                        new NuiLabel(DifficultyClass)
                        {
                            Tooltip = SkillTooltip,
                            Width = 20f,
                            Height = 38f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = SkillColor
                        },
                        new NuiSpacer { Width = 20f },
                        new NuiLabel(label: "Gold Cost:")
                        {
                            Width = 75f,
                            Height = 38f,
                            VerticalAlign = NuiVAlign.Middle
                        },
                        new NuiSpacer { Width = 20f },
                        new NuiLabel(GoldCost)
                        {
                            ForegroundColor = GoldCostColor,
                            Height = 38f,
                            Width = 100f,
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle,
                            Tooltip = GoldCostTooltip,
                            Encouraged = EncourageGold
                        }
                    }
                }
            }
        };
}
