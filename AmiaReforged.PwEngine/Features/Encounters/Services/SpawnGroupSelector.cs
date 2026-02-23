using AmiaReforged.PwEngine.Features.Encounters.Models;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.Encounters.Services;

/// <summary>
/// Selects an eligible <see cref="SpawnGroup"/> from a profile based on the current
/// <see cref="EncounterContext"/>. Groups are filtered by conditions, then one is
/// chosen via weighted random selection.
/// </summary>
[ServiceBinding(typeof(SpawnGroupSelector))]
public class SpawnGroupSelector
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly Random Rng = new();

    private readonly SpawnConditionEvaluator _evaluator;

    public SpawnGroupSelector(SpawnConditionEvaluator evaluator)
    {
        _evaluator = evaluator;
    }

    /// <summary>
    /// Selects a spawn group from the profile. Returns null if no groups are eligible.
    /// </summary>
    public SpawnGroup? SelectGroup(SpawnProfile profile, EncounterContext context)
    {
        List<SpawnGroup> eligible = profile.SpawnGroups
            .Where(g => _evaluator.AllConditionsMet(g.Conditions, context))
            .ToList();

        if (eligible.Count == 0)
        {
            Log.Debug("No eligible spawn groups for profile '{Name}' (area {Area}).",
                profile.Name, profile.AreaResRef);
            return null;
        }

        return WeightedRandomSelect(eligible);
    }

    /// <summary>
    /// Picks one group from the list using weighted random selection based on <see cref="SpawnGroup.Weight"/>.
    /// </summary>
    private static SpawnGroup WeightedRandomSelect(List<SpawnGroup> groups)
    {
        int totalWeight = groups.Sum(g => Math.Max(g.Weight, 1));
        int roll = Rng.Next(totalWeight);
        int cumulative = 0;

        foreach (SpawnGroup group in groups)
        {
            cumulative += Math.Max(group.Weight, 1);
            if (roll < cumulative)
                return group;
        }

        // Fallback (shouldn't happen with correct math)
        return groups[^1];
    }
}
