using AmiaReforged.PwEngine.Systems.Crafting.Models;
using Anvil.API;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.CraftingCategory;

public class MythalCategoryModel
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly NwItem _item;
    private readonly CraftingPropertyData _data;

    public List<MythalCategory> Categories { get; }

    private readonly MythalMap _mythals;

    public MythalCategoryModel(NwItem item, CraftingPropertyData data, NwPlayer player)
    {
        _item = item;
        _data = data;
        
        _mythals = new MythalMap(player);
        Categories = new List<MythalCategory>();

        SetupCategories();
    }

    private void SetupCategories()
    {
        int baseType = NWScript.GetBaseItemType(_item);
        IReadOnlyList<Models.CraftingCategory> internalCategories = _data.Properties[baseType];
        Log.Info("Setting up categories.");
        foreach (Models.CraftingCategory category in internalCategories)
        {
            Log.Info("Setting up category: " + category.Label);
            MythalCategory modelCategory = new()
            {
                Label = category.Label,
                Properties = new List<MythalProperty>()
            };

            foreach (CraftingProperty property in category.Properties)
            {
                if(!_mythals.Map.TryGetValue(property.CraftingTier, out int amount)) continue;
                if(amount == 0) continue;
                
                MythalProperty modelProperty = new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Label = property.GuiLabel,
                    InternalProperty = property,
                    Selectable = true
                };
                
                modelCategory.Properties.Add(modelProperty);
            }
            
            Categories.Add(modelCategory);
        }
    }

    public void UpdateFromRemainingBudget(int remainingBudget)
    {
        List<MythalProperty> properties = Categories.SelectMany(c => c.Properties).ToList();
        
        foreach (MythalProperty property in properties)
        {
            property.Selectable = property.InternalProperty.PowerCost <= remainingBudget;

            property.Color = property.Selectable ? ColorConstants.White : ColorConstants.Red;
            
            property.CostLabelTooltip = property.Selectable ? "Power Cost" : "Too expensive";
        }
    }

    public class MythalCategory
    {
        public string Label { get; set; }
        public List<MythalProperty> Properties { get; init; }
    }

    public class MythalProperty
    {
        public string Id { get; set; }
        public string Label { get; set; }
        public CraftingProperty InternalProperty { get; set; }
        public bool Selectable { get; set; }
        public Color Color { get; set; }
        public string CostLabelTooltip { get; set; }
    }
}