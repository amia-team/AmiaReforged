using Anvil.API;
using Anvil.Services;
using AmiaReforged.PwEngine.Features.AI.Core.Interfaces;
using AmiaReforged.PwEngine.Features.AI.Core.Models;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.AI.Core.Services;

/// <summary>
/// Calculates and distributes XP/gold rewards for creature kills.
/// Ported from RewardXPForKill() in inc_ds_ondeath.nss.
/// </summary>
[ServiceBinding(typeof(IXpRewardHandler))]
public class XpRewardService : IXpRewardHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly IPartyStateCalculator _partyCalculator;
    private readonly ICreatureClassifier _classifier;

    // Local variable names (matching legacy NWScript)
    private const string VarIsBoss = "is_boss";
    private const string VarXpBlock = "ds_xpbl";
    private const string VarDoubleXp = "doubleXP";

    public XpRewardService(
        IPartyStateCalculator partyCalculator,
        ICreatureClassifier classifier)
    {
        _partyCalculator = partyCalculator;
        _classifier = classifier;
    }

    /// <inheritdoc />
    public XpRewardResult CalculateAndDistributeRewards(NwCreature killedCreature, NwGameObject killer)
    {
        // Calculate party state
        PartyState partyState = _partyCalculator.CalculatePartyState(killer);

        // No PCs in party
        if (partyState.PcCount == 0)
        {
            return XpRewardResult.Blocked();
        }

        // Get creature CR (minimum 1.0)
        float monsterCr = Math.Max(killedCreature.ChallengeRating, 1.0f);

        // Check for double XP event
        int doubleXpMultiplier = GetDoubleXpMultiplier();

        // Calculate gold per PC
        int baseGold = RollGold(monsterCr);
        int goldPerPc = baseGold / partyState.PcCount;

        // Calculate XP cap (bosses bypass cap)
        bool isBoss = killedCreature.GetObjectVariable<LocalVariableInt>(VarIsBoss).Value == 1;
        int xpCap = CalculateXpCap(partyState, isBoss);

        // Calculate base XP
        float baseXp = CalculateBaseXp(monsterCr, partyState.AverageLevel, partyState.XpMultiplier);

        // Distribute rewards to party members
        List<IndividualReward> individualRewards = new List<IndividualReward>();
        int totalXp = 0;
        int totalGold = 0;

        foreach (NwPlayer player in partyState.PartyMembers)
        {
            NwCreature? creature = player.ControlledCreature;
            if (creature == null || creature.Area != partyState.Area || creature.IsDead)
            {
                continue;
            }

            IndividualReward reward = CalculateIndividualReward(
                player,
                creature,
                baseXp,
                xpCap,
                goldPerPc,
                partyState,
                doubleXpMultiplier);

            individualRewards.Add(reward);

            // Apply rewards
            if (reward.XpAwarded > 0 && string.IsNullOrEmpty(reward.BlockReason))
            {
                ApplyXpReward(creature, reward.XpAwarded);
            }

            if (reward.GoldAwarded > 0)
            {
                ApplyGoldReward(player, reward.GoldAwarded);
            }

            totalXp += reward.XpAwarded;
            totalGold += reward.GoldAwarded;
        }

        // Update module statistics
        UpdateModuleStatistics(totalXp, totalGold);

        return new XpRewardResult
        {
            TotalXpRewarded = totalXp,
            TotalGoldRewarded = totalGold,
            PartyPcCount = partyState.PcCount,
            WasBlocked = false,
            IndividualRewards = individualRewards
        };
    }

    private IndividualReward CalculateIndividualReward(
        NwPlayer player,
        NwCreature creature,
        float baseXp,
        int xpCap,
        int goldPerPc,
        PartyState partyState,
        int doubleXpMultiplier)
    {
        int level = creature.Level;
        string? blockReason = null;
        int xpAwarded;
        int goldAwarded = goldPerPc;

        // Check XP block
        bool hasXpBlock = creature.GetObjectVariable<LocalVariableInt>(VarXpBlock).Value > 0;
        if (hasXpBlock)
        {
            player.SendServerMessage("You've activated your XP block. No XP awarded.");
            return new IndividualReward
            {
                Player = player,
                XpAwarded = 0,
                GoldAwarded = goldAwarded,
                BlockReason = "XP block active"
            };
        }

        // Check level matches XP ratio (needs to level up)
        if (!GetLevelMatchesXpRatio(creature))
        {
            player.SendServerMessage("- Please level-up before hunting any more creatures. -");
            xpAwarded = 1;
            blockReason = "Needs to level up";

            // No gold for sub-30 if blocked
            if (level < 30)
            {
                goldAwarded = 0;
            }
        }
        // Max level reached
        else if (level >= 30)
        {
            player.SendServerMessage("You've reached your maximum level. 1 XP awarded.");
            xpAwarded = 1;
            blockReason = "Max level reached";
        }
        // Normal XP calculation
        else
        {
            xpAwarded = (int)baseXp;

            // Minimum 1 XP
            if (xpAwarded < 1) xpAwarded = 1;

            // Cap XP
            if (xpAwarded > xpCap) xpAwarded = xpCap;

            // Apply level difference penalty
            if (partyState.HasLevelPenalty)
            {
                float penalty = partyState.GetLevelPenalty();
                xpAwarded -= (int)(xpAwarded * penalty);
                if (xpAwarded < 1) xpAwarded = 1;
            }

            // Apply double XP multiplier
            xpAwarded *= doubleXpMultiplier;
        }

        return new IndividualReward
        {
            Player = player,
            XpAwarded = xpAwarded,
            GoldAwarded = goldAwarded,
            BlockReason = blockReason
        };
    }

    /// <summary>
    /// Checks if XP should be awarded based on XP to level ratio.
    /// Ported from GetLevelMatchesXPRatio() in inc_ds_ondeath.nss.
    /// </summary>
    private static bool GetLevelMatchesXpRatio(NwCreature creature)
    {
        int currentXp = creature.Xp;
        int level = creature.Level;

        int nextLevel = level + 1;
        int nextNextLevel = level + 2;

        // XP required for next level
        int xpLimit = 500 * nextLevel * (nextLevel - 1);
        // XP required for level after that
        int xpLimit2 = 500 * nextNextLevel * (nextNextLevel - 1);
        // Half-way point into next level
        int xpLimit3 = (xpLimit2 - xpLimit) / 2;
        int xpLimit4 = xpLimit + xpLimit3;

        // Block if current XP exceeds half-way into next level
        return currentXp < xpLimit4;
    }

    private static float CalculateBaseXp(float monsterCr, float partyLevel, float xpMultiplier)
    {
        // Formula: (25 + 5 * (CR - PartyLevel)) * Multiplier
        return (25.0f + (5.0f * (monsterCr - partyLevel))) * xpMultiplier;
    }

    private static int CalculateXpCap(PartyState partyState, bool isBoss)
    {
        if (isBoss)
        {
            return int.MaxValue; // No cap for bosses
        }

        // Cap = 50 + (PartyLevel * XPMultiplier)
        return 50 + (int)(partyState.AverageLevel * partyState.XpMultiplier + 0.5f);
    }

    private static int RollGold(float monsterCr)
    {
        // Roll d8 per CR point
        int dice = (int)monsterCr;
        int total = 0;
        Random random = new Random();
        for (int i = 0; i < dice; i++)
        {
            total += random.Next(1, 9); // d8
        }
        return total;
    }

    private static int GetDoubleXpMultiplier()
    {
        NwModule module = NwModule.Instance;
        int doubleXp = module.GetObjectVariable<LocalVariableInt>(VarDoubleXp).Value;
        return doubleXp == 1 ? 2 : 1;
    }

    private static void ApplyXpReward(NwCreature creature, int xp)
    {
        creature.Xp += xp;
    }

    private static void ApplyGoldReward(NwPlayer player, int gold)
    {
        // Use module to assign gold giving (matches legacy behavior)
        NwModule module = NwModule.Instance;
        if (player.ControlledCreature != null)
        {
            player.ControlledCreature.GiveGold(gold);
        }
    }

    private static void UpdateModuleStatistics(int xp, int gold)
    {
        NwModule module = NwModule.Instance;

        LocalVariableInt xpVar = module.GetObjectVariable<LocalVariableInt>("MonsterXP");
        xpVar.Value += xp;

        LocalVariableInt goldVar = module.GetObjectVariable<LocalVariableInt>("MonsterGold");
        goldVar.Value += gold;
    }
}
