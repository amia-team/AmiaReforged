using Anvil.API;
using Anvil.Services;
using AmiaReforged.PwEngine.Features.AI.Core.Interfaces;
using AmiaReforged.PwEngine.Features.AI.Core.Models;
using NLog;

namespace AmiaReforged.PwEngine.Features.AI.Core.Services;

/// <summary>
/// Manages loot bin containers for treasure generation.
/// Ported from InitialiseLootBin(), GetLootBin() in inc_ds_ondeath.nss.
/// </summary>
[ServiceBinding(typeof(ILootBinManager))]
public class LootBinManager : ILootBinManager
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    // Constants matching legacy NWScript
    private const string LootBinPrefix = "CD_TREASURE_";
    private const string LootBinInit = "CD_INIT";
    private const string LootBinCount = "CD_ITEM_COUNT";
    private const string ItemPrefix = "CD_ITEM_";
    private const string TreasureBinCrPrefix = "TreasureBinCR";

    // Local variable names on creatures
    private const string VarCustDropByTag = "CustDropByTag";
    private const string VarCustLootBin = "CustLootBin";
    private const string VarIsBoss = "is_boss";
    private const string VarCr = "CR";

    /// <inheritdoc />
    public NwPlaceable? GetLootBin(NwCreature creature)
    {
        int cr = (int)creature.ChallengeRating;
        bool isBoss = creature.GetObjectVariable<LocalVariableInt>(VarIsBoss).Value == 1;

        // Cap non-boss creatures at 40 CR
        if (cr > 40 && !isBoss)
        {
            cr = 40;
        }

        // Check for custom loot bin by tag
        if (creature.GetObjectVariable<LocalVariableInt>(VarCustDropByTag).Value == 1)
        {
            string tag = $"{LootBinPrefix}{creature.Tag}";
            return GetLootBinByTag(tag);
        }

        // Check for custom loot bin name
        string? customBin = creature.GetObjectVariable<LocalVariableString>(VarCustLootBin).Value;
        if (!string.IsNullOrEmpty(customBin))
        {
            string tag = $"{LootBinPrefix}{customBin}";
            return GetLootBinByTag(tag);
        }

        // Determine tier by CR
        LootTier tier = LootTierExtensions.FromChallengeRating(cr, isBoss);
        return GetLootBinByTier(tier);
    }

    /// <inheritdoc />
    public NwPlaceable? GetLootBinByTier(LootTier tier)
    {
        return GetLootBinByTag(tier.GetBinTag());
    }

    /// <inheritdoc />
    public NwPlaceable? GetLootBinByTag(string tag)
    {
        NwPlaceable? container = NwObject.FindObjectsWithTag<NwPlaceable>(tag).FirstOrDefault();
        if (container != null)
        {
            InitializeLootBin(container);
        }
        return container;
    }

    /// <inheritdoc />
    public NwItem? GetRandomItemFromBin(NwPlaceable lootBin)
    {
        if (lootBin == null) return null;

        InitializeLootBin(lootBin);

        int itemCount = lootBin.GetObjectVariable<LocalVariableInt>(LootBinCount).Value;
        if (itemCount == 0) return null;

        Random random = new Random();
        int index = random.Next(1, itemCount + 1);
        string indexKey = $"{ItemPrefix}{index}";

        NwItem? template = lootBin.GetObjectVariable<LocalVariableObject<NwItem>>(indexKey).Value;
        return template;
    }

    /// <inheritdoc />
    public void InitializeLootBin(NwPlaceable container, bool force = false)
    {
        if (container == null) return;

        // Check if already initialized
        if (container.GetObjectVariable<LocalVariableInt>(LootBinInit).Value == 1 && !force)
        {
            return;
        }

        // Index all items in the container
        int index = 0;
        foreach (NwItem item in container.Inventory.Items)
        {
            index++;
            string indexKey = $"{ItemPrefix}{index}";
            container.GetObjectVariable<LocalVariableObject<NwItem>>(indexKey).Value = item;
        }

        container.GetObjectVariable<LocalVariableInt>(LootBinInit).Value = 1;
        container.GetObjectVariable<LocalVariableInt>(LootBinCount).Value = index;
    }

    /// <summary>
    /// Gets a loot bin CR threshold from module variables.
    /// </summary>
    public static int GetLootBinCrThreshold(string binName, int defaultValue)
    {
        NwModule module = NwModule.Instance;
        string varName = $"{TreasureBinCrPrefix}{binName}";
        int value = module.GetObjectVariable<LocalVariableInt>(varName).Value;
        return value > 0 ? value : defaultValue;
    }
}
