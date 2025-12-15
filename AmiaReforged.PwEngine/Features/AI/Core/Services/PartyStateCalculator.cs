using Anvil.API;
using Anvil.Services;
using AmiaReforged.PwEngine.Features.AI.Core.Interfaces;
using AmiaReforged.PwEngine.Features.AI.Core.Models;

namespace AmiaReforged.PwEngine.Features.AI.Core.Services;

/// <summary>
/// Calculates party state for XP/loot distribution.
/// Ported from party calculation logic in RewardXPForKill() in inc_ds_ondeath.nss.
/// </summary>
[ServiceBinding(typeof(IPartyStateCalculator))]
public class PartyStateCalculator : IPartyStateCalculator
{
    private readonly ICreatureClassifier _classifier;

    public PartyStateCalculator(ICreatureClassifier classifier)
    {
        _classifier = classifier;
    }

    /// <inheritdoc />
    public PartyState CalculatePartyState(NwGameObject killer)
    {
        // Resolve the actual player if killer is an associate
        NwGameObject resolvedKiller = ResolveToPlayer(killer);
        if (resolvedKiller is not NwCreature killerCreature)
        {
            return CreateEmptyState(killer.Area);
        }

        NwArea? area = killerCreature.Area;
        List<NwPlayer> partyMembers = new List<NwPlayer>();
        float totalLevel = 0f;
        float lowestLevel = 1000f;
        float highestLevel = -10f;
        int pcCount = 0;
        int henchmenCount = 0;

        // Get faction members in the same area
        foreach (NwCreature member in killerCreature.Faction.GetMembers())
        {
            if (member.Area != area || member.IsDead) continue;

            CreatureClassification classification = _classifier.Classify(member);

            // Count PCs and possessed familiars
            if (classification == CreatureClassification.PlayerCharacter ||
                classification == CreatureClassification.PossessedFamiliar)
            {
                pcCount++;
                float level = member.Level;
                totalLevel += level;

                if (level < lowestLevel) lowestLevel = level;
                if (level > highestLevel) highestLevel = level;

                if (member.IsPlayerControlled && member.ControllingPlayer != null)
                {
                    partyMembers.Add(member.ControllingPlayer);
                }
            }
            else if (classification.IsAssociate())
            {
                henchmenCount++;
            }
        }

        // Handle empty party
        if (pcCount == 0)
        {
            return CreateEmptyState(area);
        }

        float averageLevel = totalLevel / pcCount;
        float levelDiff = highestLevel - lowestLevel;

        // Cap PC count at 6 for XP calculations
        int cappedPcCount = Math.Min(pcCount, 6);
        float xpMultiplier = CalculateXpMultiplier(cappedPcCount);

        return new PartyState
        {
            PcCount = cappedPcCount,
            HenchmenCount = henchmenCount,
            AverageLevel = averageLevel,
            LevelDifference = levelDiff,
            LowestLevel = lowestLevel,
            HighestLevel = highestLevel,
            XpMultiplier = xpMultiplier,
            Area = area,
            PartyMembers = partyMembers
        };
    }

    /// <summary>
    /// Resolves an associate to their master player.
    /// </summary>
    private NwGameObject ResolveToPlayer(NwGameObject gameObject)
    {
        if (gameObject is not NwCreature creature) return gameObject;

        NwCreature? master = creature.Master;
        return master != null ? master : gameObject;
    }

    /// <summary>
    /// Calculates XP multiplier based on party size.
    /// Ported from AR_GetXPPartyBonus() in inc_ds_ondeath.nss.
    /// </summary>
    private static float CalculateXpMultiplier(int pcCount)
    {
        return pcCount switch
        {
            2 => 1.025f,
            3 => 1.05f,
            4 => 1.075f,
            5 => 1.1f,
            _ => 1.0f
        };
    }

    private static PartyState CreateEmptyState(NwArea? area)
    {
        return new PartyState
        {
            PcCount = 0,
            HenchmenCount = 0,
            AverageLevel = 0f,
            LevelDifference = 0f,
            LowestLevel = 0f,
            HighestLevel = 0f,
            XpMultiplier = 1.0f,
            Area = area,
            PartyMembers = Array.Empty<NwPlayer>()
        };
    }
}
