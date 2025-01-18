using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ActiveProperties;
using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ChangeList;
using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.CraftingCategory;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge
{
    /// <summary>
    /// Represents the view for the Mythal Forge crafting system.
    /// </summary>
    public sealed class MythalForgeView : ScryView<MythalForgePresenter>
    {
        /// <summary>
        /// Gets the presenter associated with this view.
        /// </summary>
        public override MythalForgePresenter Presenter { get; protected set; }

        /// <summary>
        /// Gets the binding for the item name.
        /// </summary>
        public NuiBind<string> ItemName { get; } = new("item_name");
        
        public NuiBind<string> MaxPowers { get; } = new("max_powers");
        public NuiBind<string> RemainingPowers { get; } = new("remaining_powers");

        /// <summary>
        /// Gets the category view for the Mythal Forge. Public so that the presenter can access it.
        /// </summary>
        public readonly MythalCategoryView CategoryView;

        private readonly ActivePropertiesView _activePropertiesView;
        private readonly ChangelistView _changelistView;

        /// <summary>
        /// Initializes a new instance of the <see cref="MythalForgeView"/> class. Initializes the presenter and sub-views.
        /// </summary>
        /// <param name="propertyData">The crafting property data.</param>
        /// <param name="budget">The crafting budget service.</param>
        /// <param name="item">The item being crafted.</param>
        /// <param name="player">The player performing the crafting.</param>
        public MythalForgeView(CraftingPropertyData propertyData, CraftingBudgetService budget, NwItem item,
            NwPlayer player)
        {
            Presenter = new MythalForgePresenter(this, propertyData, budget, item, player);

            CategoryView = new MythalCategoryView(Presenter);
            _activePropertiesView = new ActivePropertiesView(Presenter);
            _changelistView = new ChangelistView(Presenter);
        }

        /// <summary>
        /// Defines the root layout of the Mythal Forge view. The layout is composed of several sub-views, which are
        /// created and referenced by the view in the constructor.
        /// </summary>
        /// <returns>The root layout of the view.</returns>
        public override NuiLayout RootLayout()
        {
            return new NuiColumn
            {
                Children =
                {
                    new NuiRow
                    {
                        Children =
                        {
                            new NuiTextEdit("Edit Name", ItemName, 100, false)
                            {
                                Width = 200f,
                                Height = 60f
                            },
                            new NuiButton("Apply")
                            {
                                Id = "apply_name",
                                Height = 60f
                            },
                            new NuiSpacer(),
                            new NuiLabel("Max Powers:"),
                            new NuiGroup
                            {
                                Element = new NuiLabel(MaxPowers),
                                Border = true,
                                Width = 50f,
                                Height = 50f
                            },
                            new NuiLabel("Remaining Powers:"),
                            new NuiGroup
                            {
                                Element = new NuiLabel(RemainingPowers),
                                Border = true,
                                Width = 50f,
                                Height = 50f
                            }
                        }
                    },
                    new NuiRow
                    {
                        Children =
                        {
                            CategoryView.RootLayout(),
                            _activePropertiesView.RootLayout(),
                            _changelistView.RootLayout()
                        }
                    },
                    new NuiRow
                    {
                        Children =
                        {
                            new NuiButton("Cancel")
                            {
                                Id = "cancel",
                                Width = 200f,
                                Height = 60f
                            },
                            new NuiButton("Apply")
                            {
                                Id = "apply",
                                Width = 200f,
                                Height = 60f
                            }
                        }
                    }
                }
            };
        }
    }
}