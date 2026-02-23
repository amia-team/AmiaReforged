using AmiaReforged.PwEngine.Features.Encounters.Models;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.Encounters.Services;

/// <summary>
/// Evaluates <see cref="SpawnCondition"/> instances against a runtime <see cref="EncounterContext"/>.
/// All conditions on a group must pass (AND logic) for the group to be eligible.
/// </summary>
[ServiceBinding(typeof(SpawnConditionEvaluator))]
public class SpawnConditionEvaluator
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Returns true if all conditions in the collection are satisfied by the given context.
    /// An empty condition list evaluates to true (unconditional group).
    /// </summary>
    public bool AllConditionsMet(IReadOnlyList<SpawnCondition> conditions, EncounterContext context)
    {
        return conditions.Count == 0 || conditions.All(c => Evaluate(c, context));
    }

    /// <summary>
    /// Evaluates a single condition against the context.
    /// </summary>
    public bool Evaluate(SpawnCondition condition, EncounterContext context)
    {
        try
        {
            return condition.Type switch
            {
                SpawnConditionType.TimeOfDay => EvaluateTimeOfDay(condition, context),
                SpawnConditionType.ChaosThreshold => EvaluateChaosThreshold(condition, context),
                SpawnConditionType.MinPlayerCount => EvaluateMinPlayerCount(condition, context),
                SpawnConditionType.MaxPlayerCount => EvaluateMaxPlayerCount(condition, context),
                SpawnConditionType.RegionTag => EvaluateRegionTag(condition, context),
                SpawnConditionType.Custom => true, // Custom conditions always pass (future extensibility)
                _ => true
            };
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Failed to evaluate spawn condition {Type} with value '{Value}'. Defaulting to false.",
                condition.Type, condition.Value);
            return false;
        }
    }

    /// <summary>
    /// Evaluates a time-of-day condition. Value format: "HH:mm-HH:mm" (e.g., "06:00-18:00").
    /// Supports overnight ranges (e.g., "18:00-06:00").
    /// </summary>
    private static bool EvaluateTimeOfDay(SpawnCondition condition, EncounterContext context)
    {
        string[] parts = condition.Value.Split('-');
        if (parts.Length != 2) return false;

        if (!TimeSpan.TryParse(parts[0].Trim(), out TimeSpan start)) return false;
        if (!TimeSpan.TryParse(parts[1].Trim(), out TimeSpan end)) return false;

        TimeSpan now = context.GameTime;

        // Normal range (e.g., 06:00-18:00)
        if (start <= end)
            return now >= start && now < end;

        // Overnight range (e.g., 18:00-06:00)
        return now >= start || now < end;
    }

    /// <summary>
    /// Evaluates a chaos threshold condition. Value format: "AxisName:threshold" (e.g., "Danger:50").
    /// Operator: ">=", ">", "==", "&lt;=", "&lt;".
    /// </summary>
    private static bool EvaluateChaosThreshold(SpawnCondition condition, EncounterContext context)
    {
        string[] parts = condition.Value.Split(':');
        if (parts.Length != 2) return false;

        string axisName = parts[0].Trim();
        if (!int.TryParse(parts[1].Trim(), out int threshold)) return false;

        int axisValue = GetChaosAxisValue(context.Chaos, axisName);

        return condition.Operator switch
        {
            ">=" => axisValue >= threshold,
            ">" => axisValue > threshold,
            "==" => axisValue == threshold,
            "<=" => axisValue <= threshold,
            "<" => axisValue < threshold,
            _ => false
        };
    }

    private static int GetChaosAxisValue(ChaosState chaos, string axisName)
    {
        return axisName.ToLowerInvariant() switch
        {
            "danger" => chaos.Danger,
            "corruption" => chaos.Corruption,
            "density" => chaos.Density,
            "mutation" => chaos.Mutation,
            _ => 0
        };
    }

    /// <summary>
    /// Evaluates minimum player count. Value is a simple integer string.
    /// </summary>
    private static bool EvaluateMinPlayerCount(SpawnCondition condition, EncounterContext context)
    {
        if (!int.TryParse(condition.Value, out int minCount)) return false;
        return context.PartySize >= minCount;
    }

    /// <summary>
    /// Evaluates maximum player count. Value is a simple integer string.
    /// </summary>
    private static bool EvaluateMaxPlayerCount(SpawnCondition condition, EncounterContext context)
    {
        if (!int.TryParse(condition.Value, out int maxCount)) return false;
        return context.PartySize <= maxCount;
    }

    /// <summary>
    /// Evaluates a region tag match. Value is the region tag string.
    /// </summary>
    private static bool EvaluateRegionTag(SpawnCondition condition, EncounterContext context)
    {
        if (context.RegionTag == null) return false;
        return string.Equals(context.RegionTag, condition.Value, StringComparison.OrdinalIgnoreCase);
    }
}
