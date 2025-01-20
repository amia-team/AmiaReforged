using AmiaReforged.PwEngine.Systems.Crafting.Models;
using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ActiveProperties;
using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ChangeList;
using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.MythalCategory;
using AmiaReforged.PwEngine.Systems.NwObjectHelpers;
using Anvil.API;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge;

public class MythalForgeModel
{
    public ChangeListModel ChangeListModel { get; }
    public MythalCategoryModel MythalCategoryModel { get; }
    public ActivePropertiesModel ActivePropertiesModel { get; }

    public NwItem Item { get; }
    private readonly CraftingPropertyData _data;
    private readonly CraftingBudgetService _budget;
    private readonly IReadOnlyList<CraftingCategory> _categories;

    public MythalForgeModel(NwItem item, CraftingPropertyData data, CraftingBudgetService budget, NwPlayer player)
    {
        Item = item;
        _data = data;
        _budget = budget;

        int baseType = NWScript.GetBaseItemType(item);
        _categories = data.Properties[baseType];

        MythalCategoryModel = new MythalCategoryModel(item, player, _categories);
        ChangeListModel = new ChangeListModel();
        ActivePropertiesModel = new ActivePropertiesModel(item, player, _categories);
    }

    public int MaxBudget => _budget.MythalBudgetForNwItem(Item);

    public int RemainingPowers
    {
        get
        {
            int remaining = ActivePropertiesModel.Visible
                .Where(p => !ActivePropertiesModel.Removed.Contains(p))
                .Aggregate(MaxBudget,
                    (current, prop) => current - ItemPropertyHelper.ToCraftingProperty(prop).PowerCost);

            remaining += ChangeListModel.ChangeList.Sum(change => change.State switch
            {
                ChangeListModel.ChangeState.Added => -change.Property.PowerCost,
                ChangeListModel.ChangeState.Removed => change.Property.PowerCost,
                _ => 0
            });

            return remaining;
        }
    }

    public IEnumerable<MythalCategoryModel.MythalProperty> VisibleProperties =>
        ActivePropertiesModel.GetVisibleProperties();


    public void TryAddProperty(CraftingProperty property)
    {
        if (PropertyIsInvalid(property)) return;

        ChangeListModel.AddProperty(property);
    }

    private bool PropertyIsInvalid(CraftingProperty property)
    {
        return property.PowerCost > RemainingPowers ||
               Item.ItemProperties.Any(c => ItemPropertyHelper.GameLabel(c) == property.GameLabel);
    }


    public void RefreshCategories()
    {
        MythalCategoryModel.UpdateFromRemainingBudget(RemainingPowers);

        foreach (MythalCategoryModel.MythalCategory category in MythalCategoryModel.Categories)
        {
            foreach (MythalCategoryModel.MythalProperty property in category.Properties)
            {
                property.Selectable = ActivePropertiesModel.PropertyExistsOnItem(property);
            }
        }
    }
}

public class MythalMap
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public Dictionary<CraftingTier, int> Map { get; }

    public MythalMap(NwPlayer player)
    {
        Log.Info("Getting mythals for player.");
        Map = ItemPropertyHelper.GetMythals(player);
    }
}