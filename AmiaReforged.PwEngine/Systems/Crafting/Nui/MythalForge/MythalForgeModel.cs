using AmiaReforged.PwEngine.Systems.Crafting.Models;
using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ChangeList;
using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.CraftingCategory;
using AmiaReforged.PwEngine.Systems.NwObjectHelpers;
using Anvil.API;
using NLog;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge;

public class MythalForgeModel
{
    public ChangeListModel ChangeListModel { get; }
    public MythalCategoryModel MythalCategoryModel { get; }
    public ActivePropertiesModel ActivePropertiesModel { get; }

    private readonly NwItem _item;
    private readonly List<ItemProperty> _mythalProperties = new();
    private readonly List<ItemProperty> _removedProperties = new();
    private readonly List<ChangelistEntry> _changeList = new();
    private readonly CraftingPropertyData _data;
    private readonly CraftingBudgetService _budget;

    public MythalForgeModel(NwItem item, CraftingPropertyData data, CraftingBudgetService budget, NwPlayer player)
    {
        _item = item;
        _data = data;
        _budget = budget;

        foreach (ItemProperty property in item.ItemProperties)
        {
            if (ItemPropertyHelper.CanBeRemoved(property)) continue;

            _mythalProperties.Add(property);
        }

        MythalCategoryModel = new MythalCategoryModel(item, data, player);
    }

    public int MaxBudget => _budget.MythalBudgetForNwItem(_item);

    public int RemainingPowers
    {
        get
        {
            int remaining = _mythalProperties.Where(p => !_removedProperties.Contains(p)).Aggregate(MaxBudget,
                (current, prop) => current - ItemPropertyHelper.ToCraftingProperty(prop).PowerCost);

            remaining += _changeList.Sum(change => change.State switch
            {
                ChangeState.Added => -change.Property.PowerCost,
                ChangeState.Removed => change.Property.PowerCost,
                _ => 0
            });

            return remaining;
        }
    }

    public IEnumerable<ItemProperty> VisibleProperties =>
        _mythalProperties.Where(p => !_removedProperties.Contains(p));

    public IReadOnlyList<ChangelistEntry> ChangeList => _changeList;

    public bool TryAddProperty(CraftingProperty property)
    {
        if (property.PowerCost > RemainingPowers)
            return false;

        ChangelistEntry entry = new()
        {
            Label = property.GuiLabel,
            Property = property,
            GpCost = property.CalculateGoldCost(),
            State = ChangeState.Added
        };

        _changeList.Add(entry);
        return true;
    }

    public class ChangelistEntry
    {
        public required string Label { get; set; }
        public required CraftingProperty Property { get; set; }
        public int GpCost { get; set; }
        public ChangeState State { get; set; }
    }

    public enum ChangeState
    {
        Added,
        Removed,
        Replaced
    }

    public void RecalculateCategoryAffordability() => MythalCategoryModel.UpdateFromRemainingBudget(RemainingPowers);
}

public class ChangelistModel
{
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

public class ActivePropertiesModel
{
}