using AmiaReforged.PwEngine.Systems.Crafting.Models;
using AmiaReforged.PwEngine.Systems.NwObjectHelpers;
using Anvil.API;
using NLog;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.MythalCategory;

public class MythalCategoryModel
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly IReadOnlyList<CraftingCategory> _categories;
    private readonly NwItem _item;

    private readonly MythalMap _mythals;
    private readonly NwPlayer _player;

    public MythalCategoryModel(NwItem item, NwPlayer player, IReadOnlyList<CraftingCategory> categories)
    {
        _item = item;
        _categories = categories;

        _mythals = new(player);
        _player = player;
        Categories = new();

        SetupCategories();
    }

    public List<MythalCategory> Categories { get; }
    public Dictionary<string, MythalProperty> PropertyMap { get; } = new();


    public string MinorMythals => _mythals.Map[CraftingTier.Minor].ToString();
    public string LesserMythals => _mythals.Map[CraftingTier.Lesser].ToString();
    public string GreaterMythals => _mythals.Map[CraftingTier.Greater].ToString();
    public string IntermediateMythals => _mythals.Map[CraftingTier.Intermediate].ToString();
    public string PerfectMythals => _mythals.Map[CraftingTier.Perfect].ToString();
    public string FlawlessMythals => _mythals.Map[CraftingTier.Flawless].ToString();
    public string DivineMythals => _mythals.Map[CraftingTier.Divine].ToString();

    private void SetupCategories()
    {
        IReadOnlyList<CraftingCategory> internalCategories = _categories;
        foreach (CraftingCategory category in internalCategories)
        {
            if (_player.LoginCreature is null)
            {
                Log.Info(message: "Player login creature is null.");
                return;
            }

            // Ignore this category if it is exclusive to a class and the player does not have that class
            if (category.ExclusiveToClass)
                if (_player.LoginCreature.Classes.All(c => c.Class.ClassType != category.ExclusiveClass))
                    continue;
            MythalCategory modelCategory = new()
            {
                Label = category.Label,
                Properties = new(),
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

            if (modelCategory.Properties.Count > 0) Categories.Add(modelCategory);
        }

        // sort categories alphabetically
        Categories.Sort((a, b) => string.Compare(a.Label, b.Label, StringComparison.Ordinal));
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
            if (_mythals.Map[tier] - 1 < 0) return;
            _mythals.Map[tier] -= 1;
        }
    }

    public void RefundMythal(CraftingTier tier)
    {
        if (_mythals.Map.ContainsKey(tier)) _mythals.Map[tier] += 1;
    }

    public void DestroyMythals(NwPlayer player)
    {
        if (player.LoginCreature is null)
        {
            Log.Info(message: "Player login creature is null.");
            return;
        }

        Dictionary<CraftingTier, int> current = ItemPropertyHelper.GetMythals(player);
        foreach (CraftingTier key in _mythals.Map.Keys)
        {
            Log.Info("Key: " + key);
            Log.Info("Current: " + current[key]);
            Log.Info("After Operations: " + _mythals.Map[key]);
            int amountToTake = Math.Abs(_mythals.Map[key] - current[key]);
            Log.Info("Amount to take: " + amountToTake);
            if (amountToTake <= 0) continue;

            string resRefForMythal = ItemPropertyHelper.TierToResRef(key);

            Log.Info("ResRef: " + resRefForMythal);

            List<NwItem> mythals =
                player.LoginCreature.Inventory.Items.Where(i => i.ResRef == resRefForMythal).ToList();

            for (int i = 0; i < amountToTake; i++)
            {
                mythals[i].Destroy();
            }
        }
    }

    public bool IsMythal(NwItem item) => item.ResRef.Contains(value: "mythal");

    public bool HasMythals(CraftingTier internalPropertyCraftingTier) => _mythals.Map[internalPropertyCraftingTier] > 0;

    public class MythalCategory
    {
        public string Label { get; set; }
        public List<MythalProperty> Properties { get; init; }
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
        public static implicit operator CraftingProperty(MythalProperty property) => property.Internal;
    }
}