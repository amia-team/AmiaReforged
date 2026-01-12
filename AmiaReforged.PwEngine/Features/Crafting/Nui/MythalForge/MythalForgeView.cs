﻿﻿using AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge.SubViews.ActiveProperties;
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
                    DrawList = [new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, 1600f, 900f))]
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiTextEdit(label: "Edit Name", ItemName, 100, false)
                        {
                            Width = 200f,
                            Height = 60f
                        },
                        new NuiButton(label: "Change Name")
                        {
                            Id = ApplyNameButtonId,
                            Height = 60f
                        },
                        new NuiSpacer(),
                        new NuiRow
                        {
                            Children =
                            {
                                new NuiLabel(label: "Max Powers:")
                                {
                                    ForegroundColor = new Color(50, 40, 30)
                                },
                                new NuiGroup
                                {
                                    Element = new NuiLabel(MaxPowers),
                                    Border = true,
                                    Width = 50f,
                                    Height = 50f,
                                    Margin = 2f
                                }
                            }
                        },

                        new NuiLabel(label: "Remaining Powers:")
                        {
                            ForegroundColor = new Color(50, 40, 30)
                        },
                        new NuiGroup
                        {
                            Element = new NuiLabel(RemainingPowers),
                            Border = true,
                            Width = 50f,
                            Height = 50f,
                            Margin = 2f
                        }
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiGroup
                        {
                            Element = CategoryView.RootLayout(),
                            Border = false,
                            Width = 380f,
                            Height = 420f
                        },
                        new NuiGroup
                        {
                            Element = ActivePropertiesView.RootLayout(),
                            Border = true,
                            Width = 380f,
                            Height = 420f
                        },
                        new NuiGroup
                        {
                            Element = ChangelistView.RootLayout(),
                            Border = true,
                            Width = 380f,
                            Height = 420f
                        }
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiButton(label: "Cancel")
                        {
                            Id = Cancel,
                            Width = 200f,
                            Height = 60f
                        },
                        new NuiButton(label: "Apply")
                        {
                            Id = ApplyChanges,
                            Width = 200f,
                            Height = 60f,
                            Enabled = ApplyEnabled
                        },
                        new NuiButton(label: "To Caster Weapon")
                        {
                            Id = ToCasterWeapon,
                            Width = 200f,
                            Height = 60f,
                            Enabled = ToCasterWeaponEnabled,
                            Tooltip = "Convert to a caster weapon. Incompatible properties will be removed."
                        },
                        new NuiGroup
                        {
                            Element = new NuiRow
                            {
                                Children =
                                {
                                    new NuiLabel(label: "Difficulty:")
                                    {
                                        ForegroundColor = new Color(50, 40, 30)
                                    },
                                    new NuiGroup
                                    {
                                        Element = new NuiLabel(DifficultyClass),
                                        Tooltip = SkillTooltip,
                                        Encouraged = EncourageDifficulty,
                                        ForegroundColor = SkillColor
                                    }
                                }
                            }
                        },
                        new NuiGroup
                        {
                            Element = new NuiRow
                            {
                                Children =
                                {
                                    new NuiLabel(label: "Gold Cost:")
                                    {
                                        ForegroundColor = new Color(50, 40, 30)
                                    },
                                    new NuiGroup
                                    {
                                        Element = new NuiLabel(GoldCost)
                                        {
                                            ForegroundColor = GoldCostColor,
                                            HorizontalAlign = NuiHAlign.Center,
                                            VerticalAlign = NuiVAlign.Middle,
                                            Tooltip = GoldCostTooltip,
                                            Encouraged = EncourageGold
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };
}
