using Anvil.API;
using Anvil.Services;
using AmiaReforged.PwEngine.Features.AI.Core.Interfaces;
using AmiaReforged.PwEngine.Features.AI.Core.Models;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.AI.Core.Services;

/// <summary>
/// Generates loot on creature death.
/// Ported from GenerateLoot() in inc_ds_ondeath.nss.
/// </summary>
[ServiceBinding(typeof(ILootGenerator))]
public class LootGenerationService : ILootGenerator
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly ILootBinManager _lootBinManager;
    private readonly ICreatureClassifier _classifier;

    // Local variable names
    private const string VarCustDropPercent = "CustDropPercent";
    private const string VarIsBoss = "is_boss";
    private const string VarRareLoot = "RareLoot";
    private const string VarRaiseResRef = "ds_raise";
    private const string VarRaiseCr = "ds_cr";

    // Loot bag blueprints
    private static readonly string[] LootBagBlueprints =
    {
        "ds_daloot_1", "ds_daloot_2", "ds_daloot_3",
        "ds_daloot_4", "ds_daloot_5", "ds_daloot_6"
    };

    public LootGenerationService(
        ILootBinManager lootBinManager,
        ICreatureClassifier classifier)
    {
        _lootBinManager = lootBinManager;
        _classifier = classifier;
    }

    /// <inheritdoc />
    public LootGenerationResult GenerateLoot(
        NwGameObject killedCreature,
        NwGameObject killer,
        XpRewardResult xpResult,
        bool isChest = false)
    {
        // Store raise dead info for animate dead spell
        if (killedCreature is NwCreature creature)
        {
            CreatureClassification classification = _classifier.Classify(creature);
            if (classification == CreatureClassification.Npc && creature.Area != null)
            {
                creature.Area.GetObjectVariable<LocalVariableString>(VarRaiseResRef).Value = creature.ResRef;
                creature.Area.GetObjectVariable<LocalVariableFloat>(VarRaiseCr).Value = creature.ChallengeRating;
            }
        }

        // NPCs killing NPCs don't generate loot (unless chest)
        CreatureClassification killerClassification = _classifier.Classify(killer);
        if (!isChest && !killerClassification.CanReceiveXp())
        {
            return LootGenerationResult.None();
        }

        if (killedCreature is not NwCreature deadCreature)
        {
            // Handle placeable (treasure chest)
            if (isChest && killedCreature is NwPlaceable placeable)
            {
                return GenerateChestLoot(placeable);
            }
            return LootGenerationResult.None();
        }

        List<NwItem> generatedItems = new List<NwItem>();
        NwPlaceable? lootBag = null;
        bool droppedMythal = false;
        bool droppedSpecial = false;

        // Get loot bin for this creature
        NwPlaceable? lootBin = _lootBinManager.GetLootBin(deadCreature);
        LootTier tier = LootTierExtensions.FromChallengeRating(
            deadCreature.ChallengeRating,
            deadCreature.GetObjectVariable<LocalVariableInt>(VarIsBoss).Value == 1);

        // Process existing inventory - copy droppable items to loot bag
        lootBag = ProcessCreatureInventory(deadCreature, lootBag);

        // Calculate loot drop chance
        int dropPercent = CalculateDropPercent(deadCreature, xpResult.PartyPcCount);
        if (dropPercent == -1)
        {
            // -1 means no loot drops ever
            return new LootGenerationResult
            {
                LootGenerated = lootBag != null,
                LootBag = lootBag,
                GeneratedItems = generatedItems,
                Tier = tier,
                DroppedMythal = false,
                DroppedSpecialItem = false
            };
        }

        // Roll for loot
        Random random = new Random();
        int roll = random.Next(1, 101);

        // Resolve killer to player for messages
        NwGameObject resolvedKiller = ResolveToPlayer(killer);
        if (resolvedKiller is NwCreature killerCreature && killerCreature.IsPlayerControlled)
        {
            killerCreature.ControllingPlayer?.SendServerMessage(
                $"[Check for loot: {roll} <= {dropPercent}?]");
        }

        if (roll <= dropPercent && lootBin != null)
        {
            // Generate loot from bin
            NwItem? item = GenerateLootFromBin(lootBin, deadCreature, ref lootBag);
            if (item != null)
            {
                generatedItems.Add(item);
            }

            // 5% chance for rare loot
            if (random.Next(1, 101) <= 5)
            {
                string? rareLoot = deadCreature.GetObjectVariable<LocalVariableString>(VarRareLoot).Value;
                if (!string.IsNullOrEmpty(rareLoot) && lootBag != null)
                {
                    Task<NwItem?>? rareItem = NwItem.Create(rareLoot, lootBag);
                    if (rareItem.Result != null)
                    {
                        generatedItems.Add(rareItem.Result);
                    }
                }
            }
        }

        // 0.9% chance for mythal crystal
        if (random.Next(0, 111) == 3)
        {
            lootBag = EnsureLootBag(lootBag, deadCreature);
            string mythalRef = $"mythal{tier.GetMythalSuffix()}";
            Task<NwItem?>? mythal = NwItem.Create(mythalRef, lootBag);
            if (mythal.Result != null)
            {
                generatedItems.Add(mythal.Result);
                droppedMythal = true;
            }
        }

        // Special item drops (0.1% each for specific items)
        int specialRoll = random.Next(0, 1001);
        if (specialRoll == 5)
        {
            // Bone wand
            lootBag = EnsureLootBag(lootBag, deadCreature);
            Task<NwItem?>? wand = NwItem.Create("x2_it_cfm_wand", lootBag);
            if (wand.Result != null)
            {
                generatedItems.Add(wand.Result);
                droppedSpecial = true;
            }
        }
        else if (specialRoll == 6)
        {
            // Parchment
            lootBag = EnsureLootBag(lootBag, deadCreature);
            Task<NwItem?>? parchment = NwItem.Create("x2_it_cfm_bscrl", lootBag);
            if (parchment.Result != null)
            {
                generatedItems.Add(parchment.Result);
                droppedSpecial = true;
            }
        }
        else if (specialRoll == 7 || specialRoll == 8)
        {
            // Deity ring
            lootBag = GenerateDeityRing(lootBag, deadCreature, generatedItems);
            droppedSpecial = true;
        }

        // Notify player of loot
        if (generatedItems.Count > 0 && resolvedKiller is NwCreature notifyCreature)
        {
            NWScript.FloatingTextStringOnCreature("<c�  >Your defeated foe drops some loot!</c>", notifyCreature);
        }

        return new LootGenerationResult
        {
            LootGenerated = generatedItems.Count > 0 || lootBag != null,
            LootBag = lootBag,
            GeneratedItems = generatedItems,
            Tier = tier,
            DroppedMythal = droppedMythal,
            DroppedSpecialItem = droppedSpecial
        };
    }

    private LootGenerationResult GenerateChestLoot(NwPlaceable chest)
    {
        int cr = chest.GetObjectVariable<LocalVariableInt>("CR").Value;
        LootTier tier = LootTierExtensions.FromChallengeRating(cr);
        NwPlaceable? lootBin = _lootBinManager.GetLootBinByTier(tier);

        if (lootBin == null)
        {
            return LootGenerationResult.None();
        }

        NwItem? template = _lootBinManager.GetRandomItemFromBin(lootBin);
        if (template == null)
        {
            return LootGenerationResult.None();
        }

        NwItem? copy = template.Clone(chest);
        return new LootGenerationResult
        {
            LootGenerated = copy != null,
            LootBag = null,
            GeneratedItems = copy != null ? new[] { copy } : Array.Empty<NwItem>(),
            Tier = tier,
            DroppedMythal = false,
            DroppedSpecialItem = false
        };
    }

    private NwPlaceable? ProcessCreatureInventory(NwCreature creature, NwPlaceable? lootBag)
    {
        foreach (var item in creature.Inventory.Items.ToList())
        {
            bool shouldDrop = item.Droppable || item.BaseItem.ItemType == BaseItemType.Book;

            if (shouldDrop && item.Tag != "ds_delete")
            {
                lootBag = EnsureLootBag(lootBag, creature);
                item.Clone(lootBag, copyLocalState: true);
            }

            // Always destroy the original
            item.Destroy();
        }

        return lootBag;
    }

    private int CalculateDropPercent(NwCreature creature, int partyPcCount)
    {
        int percent = creature.GetObjectVariable<LocalVariableInt>(VarCustDropPercent).Value;

        // -1 means disabled
        if (percent == -1) return -1;

        float cr = creature.ChallengeRating;
        bool isBoss = creature.GetObjectVariable<LocalVariableInt>(VarIsBoss).Value == 1;

        // Uber loot (CR 40+): 5% per party member, max 50%
        if (cr > 40.0f)
        {
            percent = 5 * partyPcCount;
            if (percent > 50) percent = 50;
        }
        else if (percent == 0)
        {
            // Default: 4 + party size
            percent = 4 + partyPcCount;
        }

        return percent;
    }

    private NwItem? GenerateLootFromBin(NwPlaceable lootBin, NwCreature creature, ref NwPlaceable? lootBag)
    {
        bool isBoss = creature.GetObjectVariable<LocalVariableInt>(VarIsBoss).Value == 1;
        Random random = new Random();

        // 10% chance for random generated item (non-boss)
        if (random.Next(1, 11) == 5 && !isBoss)
        {
            // TODO: Implement CreateRandomInLootBag equivalent (random weapon/armor/jewelry)
            // For now, fall through to standard loot bin item
        }

        // Get random item from loot bin
        NwItem? template = _lootBinManager.GetRandomItemFromBin(lootBin);
        if (template == null) return null;

        lootBag = EnsureLootBag(lootBag, creature);
        return template.Clone(lootBag, copyLocalState: true);
    }

    private NwPlaceable? GenerateDeityRing(NwPlaceable? lootBag, NwCreature creature, List<NwItem> generatedItems)
    {
        NwPlaceable? godRingBin = _lootBinManager.GetLootBinByTag("CD_TREASURE_GODRINGS");
        if (godRingBin == null) return lootBag;

        NwItem? template = _lootBinManager.GetRandomItemFromBin(godRingBin);
        if (template == null) return lootBag;

        lootBag = EnsureLootBag(lootBag, creature);
        NwItem? ring = template.Clone(lootBag, copyLocalState: true);
        if (ring != null)
        {
            generatedItems.Add(ring);
        }

        return lootBag;
    }

    private static NwPlaceable EnsureLootBag(NwPlaceable? existing, NwCreature creature)
    {
        if (existing != null) return existing;

        Random random = new Random();
        string blueprint = LootBagBlueprints[random.Next(0, LootBagBlueprints.Length)];

        return NwPlaceable.Create(blueprint, creature.Location)!;
    }

    private static NwGameObject ResolveToPlayer(NwGameObject gameObject)
    {
        if (gameObject is not NwCreature creature) return gameObject;

        NwCreature? master = creature.Master;
        return master ?? gameObject;
    }
}
