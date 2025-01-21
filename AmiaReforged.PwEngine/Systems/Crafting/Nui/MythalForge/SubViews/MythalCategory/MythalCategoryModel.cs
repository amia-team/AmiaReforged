using AmiaReforged.PwEngine.Systems.Crafting.Models;
using AmiaReforged.PwEngine.Systems.NwObjectHelpers;
using Anvil.API;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.MythalCategory;

public class MythalCategoryModel
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly NwItem _item;

    public List<MythalCategory> Categories { get; }
    public Dictionary<string, MythalProperty> PropertyMap { get; } = new();

    private readonly MythalMap _mythals;
    private readonly IReadOnlyList<CraftingCategory> _categories;

    public MythalCategoryModel(NwItem item, NwPlayer player, IReadOnlyList<CraftingCategory> categories)
    {
        _item = item;
        _categories = categories;

        _mythals = new MythalMap(player);
        Categories = new List<MythalCategory>();

        SetupCategories();
    }

    private void SetupCategories()
    {
        IReadOnlyList<CraftingCategory> internalCategories = _categories;
        foreach (CraftingCategory category in internalCategories)
        {
            MythalCategory modelCategory = new()
            {
                Label = category.Label,
                Properties = new List<MythalProperty>(),
                PerformValidation = category.PerformValidation,
                BaseDifficulty = category.BaseDifficulty
            };

            foreach (CraftingProperty property in category.Properties)
            {
                if (!_mythals.Map.TryGetValue(property.CraftingTier, out int amount)) continue;
                if (amount == 0) continue;

                MythalProperty modelProperty = new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Label = property.GuiLabel,
                    Internal = property,
                    Selectable = true,
                    Difficulty = modelCategory.BaseDifficulty * property.PowerCost
                };

                modelCategory.Properties.Add(modelProperty);
                PropertyMap.Add(modelProperty.Id, modelProperty);
            }

            if (modelCategory.Properties.Count > 0)
            {
                Categories.Add(modelCategory);
            }
        }
    }

    public void UpdateFromRemainingBudget(int remainingBudget)
    {
        List<MythalProperty> properties = Categories.SelectMany(c => c.Properties).ToList();

        foreach (MythalProperty property in properties)
        {
            int mythalsLeft = _mythals.Map[property.Internal.CraftingTier];
            property.Selectable = property.Internal.PowerCost <= remainingBudget ||
                                  property.Internal.PowerCost == 0 || mythalsLeft > 0;

            property.Color = property.Selectable ? ColorConstants.White : ColorConstants.Red;

            property.CostLabelTooltip = property.Selectable ? "Power Cost" : "Too expensive";
        }
    }

    public void ConsumeMythal(CraftingTier tier)
    {
        if (_mythals.Map.ContainsKey(tier))
        {
            if(_mythals.Map[tier] - 1 <= 0) return;
            _mythals.Map[tier] -= 1;
        }
    }
    
    public void RefundMythal(CraftingTier tier)
    {
        if (_mythals.Map.ContainsKey(tier))
        {
            _mythals.Map[tier] += 1;
        }
    }

    public void DestroyMythals(NwPlayer player)
    {
        if (player.LoginCreature is null)
        {
            Log.Info("Player login creature is null.");
            return;
        }
        
        Dictionary<CraftingTier, int> current = ItemPropertyHelper.GetMythals(player);
        foreach (CraftingTier key in _mythals.Map.Keys)
        {
            Log.Info("Key: " + key);
            Log.Info("Current: " + current[key]);
            Log.Info("After Operations: " + _mythals.Map[key]);
            int amountToTake = current[key] - _mythals.Map[key];
            Log.Info("Amount to take: " + amountToTake);
            if (amountToTake <= 0) continue;
            
            string resRefForMythal = ItemPropertyHelper.TierToResRef(key);
            
            Log.Info("ResRef: " + resRefForMythal);
            
            List<NwItem> mythals = player.LoginCreature.Inventory.Items.Where(i => i.ResRef == resRefForMythal).ToList();
            
            for(int i = 0; i < amountToTake; i++)
            {
                mythals[i].Destroy();
            }
        }
    }
    public bool IsMythal(NwItem item)
    {
        return item.ResRef.Contains("mythal");
    }

    public bool HasMythals(CraftingTier internalPropertyCraftingTier)
    {
        return _mythals.Map[internalPropertyCraftingTier] > 0;
    }

    public class MythalCategory
    {
        public string Label { get; set; }
        public List<MythalProperty> Properties { get; init; }
        
        public Func<CraftingProperty, NwItem, PropertyValidationResult>? PerformValidation { get; set; }
        public int BaseDifficulty { get; set; }
    }

    public class MythalProperty
    {
        public string Id { get; set; }
        public string Label { get; set; }
        public CraftingProperty Internal { get; set; }
        public bool Selectable { get; set; }
        public Color Color { get; set; }
        public string CostLabelTooltip { get; set; }
        
        public int Difficulty { get; set; }
        
        // operator for converting to crafting property
        public static implicit operator CraftingProperty(MythalProperty property)
        {
            return property.Internal;
        }
    }
}