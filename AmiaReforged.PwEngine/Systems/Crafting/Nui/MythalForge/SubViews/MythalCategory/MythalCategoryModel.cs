﻿using AmiaReforged.PwEngine.Systems.Crafting.Models;
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
                    InternalProperty = property,
                    Selectable = true
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
            property.Selectable = property.InternalProperty.PowerCost <= remainingBudget ||
                                  property.InternalProperty.PowerCost == 0;

            property.Color = property.Selectable ? ColorConstants.White : ColorConstants.Red;

            property.CostLabelTooltip = property.Selectable ? "Power Cost" : "Too expensive";
        }
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
        public CraftingProperty InternalProperty { get; set; }
        public bool Selectable { get; set; }
        public Color Color { get; set; }
        public string CostLabelTooltip { get; set; }
        
        // operator for converting to crafting property
        public static implicit operator CraftingProperty(MythalProperty property)
        {
            return property.InternalProperty;
        }
    }
}