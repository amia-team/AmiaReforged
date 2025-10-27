using Anvil.API;
using AmiaReforged.PwEngine.Features.Module;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.ItemTool;

/// <summary>
/// Contains valid model/icon indices for various item types.
/// This is easily modifiable when new models are added to the module.
/// Based on base NWN EE models available in baseitems.2da and model files.
/// </summary>
public static class ItemModelValidation
{
    /// <summary>
    /// Dictionary of valid model indices per BaseItemType.
    /// Key: BaseItemType, Value: HashSet of valid model indices.
    /// </summary>
    private static readonly Dictionary<BaseItemType, HashSet<int>> ValidModels = new()
    {
        // Shields - based on base game shield models
        // Small shields
        [BaseItemType.SmallShield] = new HashSet<int>(
            Enumerable.Range(11, 6) // 11-16
                .Concat(Enumerable.Range(21, 3)) // 21-23
                .Concat(Enumerable.Range(31, 3)) // 31-33
                .Concat(Enumerable.Range(41, 9)) // 41-49
        ),

        // Large shields
        [BaseItemType.LargeShield] = new HashSet<int>(
            Enumerable.Range(11, 6) // 11-16
                .Concat(Enumerable.Range(21, 6)) // 21-26
                .Concat(Enumerable.Range(31, 3)) // 31-33
                .Concat(Enumerable.Range(41, 3)) // 41-43
                .Concat(Enumerable.Range(81, 35)) // 81-115
                .Concat(Enumerable.Range(117, 2)) // 117-118
        ),

        // Tower shields
        [BaseItemType.TowerShield] = new HashSet<int>(
            Enumerable.Range(11, 3) // 11-13
                .Concat(Enumerable.Range(21, 3)) // 21-23
                .Concat(Enumerable.Range(31, 6)) // 31-36
                .Concat(Enumerable.Range(41, 9)) // 41-49
                .Concat(Enumerable.Range(51, 4)) // 51-54
                .Concat(Enumerable.Range(61, 3)) // 61-63
                .Concat(new[] { 80 }) // 80
                .Concat(Enumerable.Range(111, 15)) // 111-125
                .Concat(new[] { 131 }) // 131
                .Concat(Enumerable.Range(151, 12)) // 151-162
                .Concat(Enumerable.Range(164, 36)) // 164-199
        ),

        // Misc items - based on your existing logic
        [BaseItemType.MiscLarge] = new HashSet<int>(Enumerable.Range(1, 16)
            .Concat(Enumerable.Range(18, 15)) // 18-32
        ),
        [BaseItemType.MiscMedium] = new HashSet<int>(Enumerable.Range(1, 127)
            .Concat(Enumerable.Range(151, 15))
        ),
        [BaseItemType.MiscSmall] = new HashSet<int>(Enumerable.Range(1, 255)),
        [BaseItemType.MiscThin] = new HashSet<int>(Enumerable.Range(1, 102)),

        // Accessories - common ranges for base game
        [BaseItemType.Amulet] = new HashSet<int>(Enumerable.Range(1, 120)
                .Concat(Enumerable.Range(169, 56)) // 166-224
        ),
        [BaseItemType.Belt] = new HashSet<int>(Enumerable.Range(1, 50)),
        [BaseItemType.Book] = new HashSet<int>(Enumerable.Range(1, 53)),
        [BaseItemType.Bracer] = new HashSet<int>(Enumerable.Range(1, 48)),
        [BaseItemType.Gem] = new HashSet<int>(Enumerable.Range(1, 37)
            .Concat(Enumerable.Range(39, 36)) // 39-74
            .Concat(Enumerable.Range(76, 26)) // 76-101
            .Concat(Enumerable.Range(231, 8)) // 231-238
        ),
        [BaseItemType.Gloves] = new HashSet<int>(Enumerable.Range(1, 60)),
        [BaseItemType.Ring] = new HashSet<int>(Enumerable.Range(1, 122)),
        [BaseItemType.LargeBox] = new HashSet<int>(Enumerable.Range(1, 30)),
    };

    /// <summary>
    /// Special handling for items identified by numeric base ID rather than BaseItemType enum.
    /// Key: base item ID, Value: HashSet of valid model indices.
    /// </summary>
    private static readonly Dictionary<uint, HashSet<int>> ValidModelsByNumericId = new()
    {
        [119] = new HashSet<int>(Enumerable.Range(1, 255)), // Misc Small 2
        [120] = new HashSet<int>(Enumerable.Range(1, 234)), // Misc Small 3
        [121] = new HashSet<int>(Enumerable.Range(1, 67)), // Misc Medium 2
        [122] = new HashSet<int>( Enumerable.Range(1, 67) // Medals
            .Concat(Enumerable.Range(69, 7)) // 69-75
        ),
        [213] = new HashSet<int>(Enumerable.Range(1, 14)),// Special Tools
    };

    /// <summary>
    /// Check if a specific model index is valid for the given item.
    /// </summary>
    /// <param name="item">The item to check</param>
    /// <param name="modelIndex">The model index to validate</param>
    /// <returns>True if the model index is valid for this item type</returns>
    public static bool IsValidModelIndex(NwItem item, int modelIndex)
    {
        // First check by numeric base ID (for special cases)
        uint baseId = item.BaseItem.Id;
        if (ValidModelsByNumericId.TryGetValue(baseId, out HashSet<int>? numericSet))
        {
            return numericSet.Contains(modelIndex);
        }

        // Then check by BaseItemType
        BaseItemType itemType = item.BaseItem.ItemType;
        if (ValidModels.TryGetValue(itemType, out HashSet<int>? modelSet))
        {
            return modelSet.Contains(modelIndex);
        }

        return false;
    }

    /// <summary>
    /// Get the maximum valid model index for an item, or 0 if not supported.
    /// </summary>
    public static int GetMaxModelIndex(NwItem item)
    {
        uint baseId = item.BaseItem.Id;
        if (ValidModelsByNumericId.TryGetValue(baseId, out HashSet<int>? numericSet))
        {
            return numericSet.Max();
        }

        BaseItemType itemType = item.BaseItem.ItemType;
        if (ValidModels.TryGetValue(itemType, out HashSet<int>? modelSet))
        {
            return modelSet.Max();
        }

        return 0;
    }

    /// <summary>
    /// Get all valid model indices for an item type.
    /// </summary>
    public static IEnumerable<int> GetValidIndices(NwItem item)
    {
        uint baseId = item.BaseItem.Id;
        if (ValidModelsByNumericId.TryGetValue(baseId, out HashSet<int>? numericSet))
        {
            return numericSet.OrderBy(x => x);
        }

        BaseItemType itemType = item.BaseItem.ItemType;
        if (ValidModels.TryGetValue(itemType, out HashSet<int>? modelSet))
        {
            return modelSet.OrderBy(x => x);
        }

        return Enumerable.Empty<int>();
    }

    /// <summary>
    /// Check if an item type supports model changes.
    /// </summary>
    public static bool SupportsModelChanges(NwItem item)
    {
        uint baseId = item.BaseItem.Id;
        if (ValidModelsByNumericId.ContainsKey(baseId))
            return true;

        BaseItemType itemType = item.BaseItem.ItemType;
        return ValidModels.ContainsKey(itemType);
    }

    // ========================================
    // Easy-to-modify section for adding new models
    // ========================================

    /// <summary>
    /// Add a single valid model index for a specific base item type.
    /// Use this when you add new custom models to your module.
    /// </summary>
    public static void AddValidModel(BaseItemType itemType, int modelIndex)
    {
        if (!ValidModels.ContainsKey(itemType))
        {
            ValidModels[itemType] = new HashSet<int>();
        }

        ValidModels[itemType].Add(modelIndex);
    }

    /// <summary>
    /// Add a range of valid model indices for a specific base item type.
    /// Use this when you add multiple new custom models to your module.
    /// </summary>
    public static void AddValidModelRange(BaseItemType itemType, int startInclusive, int endInclusive)
    {
        if (!ValidModels.ContainsKey(itemType))
        {
            ValidModels[itemType] = new HashSet<int>();
        }

        for (int i = startInclusive; i <= endInclusive; i++)
        {
            ValidModels[itemType].Add(i);
        }
    }

    /// <summary>
    /// Add multiple specific valid model indices for a specific base item type.
    /// Use this for non-contiguous model numbers (e.g., you have models 51, 59, 73 but not 52-58 or 60-72).
    /// Example: AddValidModels(BaseItemType.TowerShield, 51, 59, 73, 88);
    /// </summary>
    public static void AddValidModels(BaseItemType itemType, params int[] modelIndices)
    {
        if (!ValidModels.ContainsKey(itemType))
        {
            ValidModels[itemType] = new HashSet<int>();
        }

        foreach (int index in modelIndices)
        {
            ValidModels[itemType].Add(index);
        }
    }

    /// <summary>
    /// Add multiple specific valid model indices for a specific base item type from a collection.
    /// Use this for non-contiguous model numbers when you have them in a list or array.
    /// </summary>
    public static void AddValidModels(BaseItemType itemType, IEnumerable<int> modelIndices)
    {
        if (!ValidModels.ContainsKey(itemType))
        {
            ValidModels[itemType] = new HashSet<int>();
        }

        foreach (int index in modelIndices)
        {
            ValidModels[itemType].Add(index);
        }
    }

    /// <summary>
    /// Add valid models for a numeric base item ID.
    /// Use this for special item types identified by numeric ID.
    /// </summary>
    public static void AddValidModelByNumericId(uint baseId, int modelIndex)
    {
        if (!ValidModelsByNumericId.ContainsKey(baseId))
        {
            ValidModelsByNumericId[baseId] = new HashSet<int>();
        }

        ValidModelsByNumericId[baseId].Add(modelIndex);
    }

    /// <summary>
    /// Add multiple valid models for a numeric base item ID.
    /// Use this for special item types identified by numeric ID when you have non-contiguous model numbers.
    /// Example: AddValidModelsByNumericId(119, 51, 59, 73, 88);
    /// </summary>
    public static void AddValidModelsByNumericId(uint baseId, params int[] modelIndices)
    {
        if (!ValidModelsByNumericId.ContainsKey(baseId))
        {
            ValidModelsByNumericId[baseId] = new HashSet<int>();
        }

        foreach (int index in modelIndices)
        {
            ValidModelsByNumericId[baseId].Add(index);
        }
    }

    // ========================================
    // Amia Custom Base Item Type Support
    // ========================================

    /// <summary>
    /// Add a single valid model index for a custom Amia base item type.
    /// Example: AddValidModel(AmiaBaseItemType.MiscSmall2, 255);
    /// </summary>
    public static void AddValidModel(AmiaBaseItemType itemType, int modelIndex)
    {
        uint baseId = (uint)itemType;
        if (!ValidModelsByNumericId.ContainsKey(baseId))
        {
            ValidModelsByNumericId[baseId] = new HashSet<int>();
        }
        ValidModelsByNumericId[baseId].Add(modelIndex);
    }

    /// <summary>
    /// Add a range of valid model indices for a custom Amia base item type.
    /// Example: AddValidModelRange(AmiaBaseItemType.MiscSmall2, 255, 300);
    /// </summary>
    public static void AddValidModelRange(AmiaBaseItemType itemType, int startInclusive, int endInclusive)
    {
        uint baseId = (uint)itemType;
        if (!ValidModelsByNumericId.ContainsKey(baseId))
        {
            ValidModelsByNumericId[baseId] = new HashSet<int>();
        }

        for (int i = startInclusive; i <= endInclusive; i++)
        {
            ValidModelsByNumericId[baseId].Add(i);
        }
    }

    /// <summary>
    /// Add multiple specific valid model indices for a custom Amia base item type.
    /// Example: AddValidModels(AmiaBaseItemType.MiscSmall2, 255, 260, 275, 300);
    /// </summary>
    public static void AddValidModels(AmiaBaseItemType itemType, params int[] modelIndices)
    {
        uint baseId = (uint)itemType;
        if (!ValidModelsByNumericId.ContainsKey(baseId))
        {
            ValidModelsByNumericId[baseId] = new HashSet<int>();
        }

        foreach (int index in modelIndices)
        {
            ValidModelsByNumericId[baseId].Add(index);
        }
    }

    /// <summary>
    /// Add multiple specific valid model indices for a custom Amia base item type from a collection.
    /// Example: AddValidModels(AmiaBaseItemType.MiscSmall2, modelList);
    /// </summary>
    public static void AddValidModels(AmiaBaseItemType itemType, IEnumerable<int> modelIndices)
    {
        uint baseId = (uint)itemType;
        if (!ValidModelsByNumericId.ContainsKey(baseId))
        {
            ValidModelsByNumericId[baseId] = new HashSet<int>();
        }

        foreach (int index in modelIndices)
        {
            ValidModelsByNumericId[baseId].Add(index);
        }
    }
}
