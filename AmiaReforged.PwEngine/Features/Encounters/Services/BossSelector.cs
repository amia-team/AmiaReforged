using AmiaReforged.PwEngine.Features.Encounters.Models;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.Encounters.Services;

/// <summary>
/// Selects an eligible <see cref="BossConfig"/> from a profile's boss pool based on the
/// current <see cref="EncounterContext"/>. Bosses are filtered by <see cref="BossConfig.IsActive"/>,
/// then by conditions, then one is chosen via weighted random selection.
/// </summary>
[ServiceBinding(typeof(BossSelector))]
public class BossSelector
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly Random Rng = new();

    private readonly SpawnConditionEvaluator _evaluator;

    public BossSelector(SpawnConditionEvaluator evaluator)
    {
        _evaluator = evaluator;
    }

    /// <summary>
    /// Selects a boss from the profile's boss pool. Returns null if no bosses are eligible.
    /// Only active bosses whose conditions all pass are considered.
    /// </summary>
    public BossConfig? SelectBoss(SpawnProfile profile, EncounterContext context)
    {
        List<BossConfig> eligible = profile.BossConfigs
            .Where(b => b.IsActive)
            .Where(b => _evaluator.AllConditionsMet(b.Conditions, context))
            .ToList();

        if (eligible.Count == 0)
        {
            Log.Debug("No eligible bosses for profile '{Name}' (area {Area}).",
                profile.Name, profile.AreaResRef);
            return null;
        }

        return WeightedRandomSelect(eligible);
    }

    /// <summary>
    /// Picks one boss from the list using weighted random selection based on <see cref="BossConfig.Weight"/>.
    /// </summary>
    private static BossConfig WeightedRandomSelect(List<BossConfig> bosses)
    {
        int totalWeight = bosses.Sum(b => Math.Max(b.Weight, 1));
        int roll = Rng.Next(totalWeight);
        int cumulative = 0;

        foreach (BossConfig boss in bosses)
        {
            cumulative += Math.Max(boss.Weight, 1);
            if (roll < cumulative)
                return boss;
        }

        // Fallback (shouldn't happen with correct math)
        return bosses[^1];
    }
}
