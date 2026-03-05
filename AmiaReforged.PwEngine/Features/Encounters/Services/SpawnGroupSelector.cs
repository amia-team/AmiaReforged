using AmiaReforged.PwEngine.Features.Encounters.Models;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.Encounters.Services;

/// <summary>
/// Selects an eligible <see cref="SpawnGroup"/> from a profile based on the current
/// <see cref="EncounterContext"/>. Selection uses a 3-tier priority system:
///   1. Most-specific conditioned groups (highest condition count, all conditions pass)
///   2. Less-specific conditioned groups (fewer conditions, all pass)
///   3. Generic groups (no conditions) — fallback only
/// Within a tier, groups are chosen via weighted random selection.
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
        return SelectGroup(profile, context, filter: null);
    }

    /// <summary>
    /// Selects a spawn group from the profile, optionally filtering by a predicate
    /// (e.g. to include only trigger-based or only area-enter groups).
    /// Returns null if no groups are eligible.
    /// 
    /// Selection priority:
    ///   1. Conditioned groups whose conditions all pass, preferring the most-specific
    ///      (highest <see cref="SpawnGroup.Conditions"/> count). Tied specificity is
    ///      resolved by weighted random.
    ///   2. Generic (no-condition) groups — only used when zero conditioned groups match.
    /// </summary>
    public SpawnGroup? SelectGroup(SpawnProfile profile, EncounterContext context,
        Func<SpawnGroup, bool>? filter)
    {
        IEnumerable<SpawnGroup> candidates = profile.SpawnGroups;
        if (filter != null)
            candidates = candidates.Where(filter);

        List<SpawnGroup> eligible = candidates
            .Where(g => _evaluator.AllConditionsMet(g.Conditions, context))
            .ToList();

        if (eligible.Count == 0)
        {
            Log.Debug("No eligible spawn groups for profile '{Name}' (area {Area}).",
                profile.Name, profile.AreaResRef);
            return null;
        }

        // Split into conditioned (have at least one condition) and generic (no conditions)
        List<SpawnGroup> conditioned = eligible.Where(g => g.Conditions.Count > 0).ToList();
        List<SpawnGroup> generic = eligible.Where(g => g.Conditions.Count == 0).ToList();

        if (conditioned.Count > 0)
        {
            // Pick the most-specific tier (highest condition count)
            int maxConditions = conditioned.Max(g => g.Conditions.Count);
            List<SpawnGroup> mostSpecific = conditioned.Where(g => g.Conditions.Count == maxConditions).ToList();

            Log.Debug("Selecting from {Count} most-specific group(s) ({Conditions} conditions) " +
                       "for profile '{Name}'. {Generic} generic group(s) skipped.",
                mostSpecific.Count, maxConditions, profile.Name, generic.Count);

            return WeightedRandomSelect(mostSpecific);
        }

        // Fallback to generic groups
        Log.Debug("No conditioned groups matched for profile '{Name}'. " +
                   "Falling back to {Count} generic group(s).",
            profile.Name, generic.Count);

        return WeightedRandomSelect(generic);
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
