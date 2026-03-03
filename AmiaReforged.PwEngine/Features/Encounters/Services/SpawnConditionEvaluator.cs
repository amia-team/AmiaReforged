using AmiaReforged.PwEngine.Features.Encounters.Models;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Encounters.Services;

/// <summary>
/// Evaluates <see cref="SpawnCondition"/> and <see cref="BossCondition"/> instances against
/// a runtime <see cref="EncounterContext"/>. All conditions on a group/boss must pass
/// (AND logic) for the group/boss to be eligible.
/// </summary>
[ServiceBinding(typeof(SpawnConditionEvaluator))]
public class SpawnConditionEvaluator
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Returns true if all spawn conditions are satisfied by the given context.
    /// An empty condition list evaluates to true (unconditional group).
    /// </summary>
    public bool AllConditionsMet(IReadOnlyList<SpawnCondition> conditions, EncounterContext context)
    {
        return conditions.Count == 0 || conditions.All(c => Evaluate(c, context));
    }

    /// <summary>
    /// Returns true if all boss conditions are satisfied by the given context.
    /// An empty condition list evaluates to true (unconditional boss).
    /// </summary>
    public bool AllConditionsMet(IReadOnlyList<BossCondition> conditions, EncounterContext context)
    {
        return conditions.Count == 0 || conditions.All(c => Evaluate(c, context));
    }

    /// <summary>
    /// Evaluates a single spawn condition against the context.
    /// </summary>
    public bool Evaluate(SpawnCondition condition, EncounterContext context)
    {
        return EvaluateCore(condition.Type, condition.Operator, condition.Value, context);
    }

    /// <summary>
    /// Evaluates a single boss condition against the context.
    /// </summary>
    public bool Evaluate(BossCondition condition, EncounterContext context)
    {
        return EvaluateCore(condition.Type, condition.Operator, condition.Value, context);
    }

    /// <summary>
    /// Core evaluation logic shared by all condition entity types.
    /// </summary>
    private bool EvaluateCore(SpawnConditionType type, string op, string value, EncounterContext context)
    {
        try
        {
            return type switch
            {
                SpawnConditionType.TimeOfDay => EvaluateTimeOfDay(value, context),
                SpawnConditionType.ChaosThreshold => EvaluateChaosThreshold(op, value, context),
                SpawnConditionType.MinPlayerCount => EvaluateMinPlayerCount(value, context),
                SpawnConditionType.MaxPlayerCount => EvaluateMaxPlayerCount(value, context),
                SpawnConditionType.RegionTag => EvaluateRegionTag(value, context),
                SpawnConditionType.TriggerTag => EvaluateTriggerTag(op, value, context),
                SpawnConditionType.TriggerLocalVariableInt => EvaluateTriggerLocalInt(op, value, context),
                SpawnConditionType.TriggerLocalVariableFloat => EvaluateTriggerLocalFloat(op, value, context),
                SpawnConditionType.TriggerLocalVariableString => EvaluateTriggerLocalString(op, value, context),
                SpawnConditionType.AreaLocalVariableInt => EvaluateAreaLocalInt(op, value, context),
                SpawnConditionType.AreaLocalVariableFloat => EvaluateAreaLocalFloat(op, value, context),
                SpawnConditionType.AreaLocalVariableString => EvaluateAreaLocalString(op, value, context),
                SpawnConditionType.Custom => true, // Custom conditions always pass (future extensibility)
                _ => true
            };
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Failed to evaluate spawn condition {Type} with value '{Value}'. Defaulting to false.",
                type, value);
            return false;
        }
    }

    // ==================== Original Condition Types ====================

    /// <summary>
    /// Evaluates a time-of-day condition. Value format: "HH:mm-HH:mm" (e.g., "06:00-18:00").
    /// Supports overnight ranges (e.g., "18:00-06:00").
    /// </summary>
    private static bool EvaluateTimeOfDay(string value, EncounterContext context)
    {
        string[] parts = value.Split('-');
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
    /// </summary>
    private static bool EvaluateChaosThreshold(string op, string value, EncounterContext context)
    {
        string[] parts = value.Split(':');
        if (parts.Length != 2) return false;

        string axisName = parts[0].Trim();
        if (!int.TryParse(parts[1].Trim(), out int threshold)) return false;

        int axisValue = GetChaosAxisValue(context.Chaos, axisName);

        return op switch
        {
            ">=" => axisValue >= threshold,
            ">" => axisValue > threshold,
            "==" => axisValue == threshold,
            "!=" => axisValue != threshold,
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
    private static bool EvaluateMinPlayerCount(string value, EncounterContext context)
    {
        if (!int.TryParse(value, out int minCount)) return false;
        return context.PartySize >= minCount;
    }

    /// <summary>
    /// Evaluates maximum player count. Value is a simple integer string.
    /// </summary>
    private static bool EvaluateMaxPlayerCount(string value, EncounterContext context)
    {
        if (!int.TryParse(value, out int maxCount)) return false;
        return context.PartySize <= maxCount;
    }

    /// <summary>
    /// Evaluates a region tag match. Value is the region tag string.
    /// </summary>
    private static bool EvaluateRegionTag(string value, EncounterContext context)
    {
        if (context.RegionTag == null) return false;
        return string.Equals(context.RegionTag, value, StringComparison.OrdinalIgnoreCase);
    }

    // ==================== Trigger Tag ====================

    /// <summary>
    /// Evaluates the spawn trigger's tag. Operators: ==, !=, contains, startswith.
    /// </summary>
    private static bool EvaluateTriggerTag(string op, string value, EncounterContext context)
    {
        if (context.Trigger == null) return false;
        string triggerTag = context.Trigger.Tag ?? "";
        return EvaluateStringComparison(op, triggerTag, value);
    }

    // ==================== Trigger Local Variables ====================

    /// <summary>
    /// Evaluates a local int variable on the spawn trigger.
    /// Value format: "VariableName:CompareValue". For % operator: "VariableName:Divisor:Remainder".
    /// </summary>
    private static bool EvaluateTriggerLocalInt(string op, string value, EncounterContext context)
    {
        if (context.Trigger == null) return false;
        if (!TryParseVarNameAndIntValue(op, value, out string varName, out int compareValue)) return false;

        int actual = NWScript.GetLocalInt(context.Trigger, varName);

        if (op == "%")
            return EvaluateModuloInt(value, actual);

        return EvaluateIntComparison(op, actual, compareValue);
    }

    /// <summary>
    /// Evaluates a local float variable on the spawn trigger.
    /// Value format: "VariableName:CompareValue". For % operator: "VariableName:Divisor:Remainder".
    /// </summary>
    private static bool EvaluateTriggerLocalFloat(string op, string value, EncounterContext context)
    {
        if (context.Trigger == null) return false;
        if (!TryParseVarNameAndFloatValue(op, value, out string varName, out float compareValue)) return false;

        float actual = NWScript.GetLocalFloat(context.Trigger, varName);

        if (op == "%")
            return EvaluateModuloFloat(value, actual);

        return EvaluateFloatComparison(op, actual, compareValue);
    }

    /// <summary>
    /// Evaluates a local string variable on the spawn trigger.
    /// Value format: "VariableName:CompareValue".
    /// </summary>
    private static bool EvaluateTriggerLocalString(string op, string value, EncounterContext context)
    {
        if (context.Trigger == null) return false;
        if (!TryParseVarNameAndStringValue(value, out string varName, out string compareValue)) return false;

        string actual = NWScript.GetLocalString(context.Trigger, varName);
        return EvaluateStringComparison(op, actual, compareValue);
    }

    // ==================== Area Local Variables ====================

    /// <summary>
    /// Evaluates a local int variable on the area.
    /// Value format: "VariableName:CompareValue". For % operator: "VariableName:Divisor:Remainder".
    /// </summary>
    private static bool EvaluateAreaLocalInt(string op, string value, EncounterContext context)
    {
        if (context.Area == null) return false;
        if (!TryParseVarNameAndIntValue(op, value, out string varName, out int compareValue)) return false;

        int actual = NWScript.GetLocalInt(context.Area, varName);

        if (op == "%")
            return EvaluateModuloInt(value, actual);

        return EvaluateIntComparison(op, actual, compareValue);
    }

    /// <summary>
    /// Evaluates a local float variable on the area.
    /// Value format: "VariableName:CompareValue". For % operator: "VariableName:Divisor:Remainder".
    /// </summary>
    private static bool EvaluateAreaLocalFloat(string op, string value, EncounterContext context)
    {
        if (context.Area == null) return false;
        if (!TryParseVarNameAndFloatValue(op, value, out string varName, out float compareValue)) return false;

        float actual = NWScript.GetLocalFloat(context.Area, varName);

        if (op == "%")
            return EvaluateModuloFloat(value, actual);

        return EvaluateFloatComparison(op, actual, compareValue);
    }

    /// <summary>
    /// Evaluates a local string variable on the area.
    /// Value format: "VariableName:CompareValue".
    /// </summary>
    private static bool EvaluateAreaLocalString(string op, string value, EncounterContext context)
    {
        if (context.Area == null) return false;
        if (!TryParseVarNameAndStringValue(value, out string varName, out string compareValue)) return false;

        string actual = NWScript.GetLocalString(context.Area, varName);
        return EvaluateStringComparison(op, actual, compareValue);
    }

    // ==================== Shared Comparison Helpers ====================

    /// <summary>
    /// Parses "VariableName:CompareValue" from the condition value for int comparisons.
    /// For non-modulo operators, expects exactly 2 colon-separated parts.
    /// </summary>
    private static bool TryParseVarNameAndIntValue(string op, string value, out string varName, out int compareValue)
    {
        varName = "";
        compareValue = 0;

        // For modulo, the value parsing is handled separately
        if (op == "%")
        {
            string[] modParts = value.Split(':');
            if (modParts.Length < 3) return false;
            varName = modParts[0].Trim();
            return !string.IsNullOrEmpty(varName);
        }

        string[] parts = value.Split(':');
        if (parts.Length != 2) return false;

        varName = parts[0].Trim();
        return !string.IsNullOrEmpty(varName) && int.TryParse(parts[1].Trim(), out compareValue);
    }

    /// <summary>
    /// Parses "VariableName:CompareValue" from the condition value for float comparisons.
    /// </summary>
    private static bool TryParseVarNameAndFloatValue(string op, string value, out string varName, out float compareValue)
    {
        varName = "";
        compareValue = 0f;

        if (op == "%")
        {
            string[] modParts = value.Split(':');
            if (modParts.Length < 3) return false;
            varName = modParts[0].Trim();
            return !string.IsNullOrEmpty(varName);
        }

        string[] parts = value.Split(':');
        if (parts.Length != 2) return false;

        varName = parts[0].Trim();
        return !string.IsNullOrEmpty(varName) && float.TryParse(parts[1].Trim(), out compareValue);
    }

    /// <summary>
    /// Parses "VariableName:CompareValue" for string comparisons.
    /// Everything after the first colon is the compare value (allows colons in values).
    /// </summary>
    private static bool TryParseVarNameAndStringValue(string value, out string varName, out string compareValue)
    {
        varName = "";
        compareValue = "";

        int colonIdx = value.IndexOf(':');
        if (colonIdx < 1) return false;

        varName = value[..colonIdx].Trim();
        compareValue = value[(colonIdx + 1)..];
        return !string.IsNullOrEmpty(varName);
    }

    /// <summary>
    /// Compares two integers using the given operator.
    /// </summary>
    private static bool EvaluateIntComparison(string op, int actual, int expected)
    {
        return op switch
        {
            "==" => actual == expected,
            "!=" => actual != expected,
            ">" => actual > expected,
            ">=" => actual >= expected,
            "<" => actual < expected,
            "<=" => actual <= expected,
            _ => false
        };
    }

    /// <summary>
    /// Compares two floats using the given operator.
    /// Uses a small epsilon for equality checks.
    /// </summary>
    private static bool EvaluateFloatComparison(string op, float actual, float expected)
    {
        const float epsilon = 0.0001f;
        return op switch
        {
            "==" => Math.Abs(actual - expected) < epsilon,
            "!=" => Math.Abs(actual - expected) >= epsilon,
            ">" => actual > expected,
            ">=" => actual >= expected,
            "<" => actual < expected,
            "<=" => actual <= expected,
            _ => false
        };
    }

    /// <summary>
    /// Compares two strings using the given operator. Case-insensitive.
    /// </summary>
    private static bool EvaluateStringComparison(string op, string actual, string expected)
    {
        return op.ToLowerInvariant() switch
        {
            "==" => string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase),
            "!=" => !string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase),
            "contains" => actual.Contains(expected, StringComparison.OrdinalIgnoreCase),
            "startswith" => actual.StartsWith(expected, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    /// <summary>
    /// Evaluates modulo for integers. Value format: "VarName:Divisor:Remainder".
    /// Returns true if actual % divisor == remainder.
    /// </summary>
    private static bool EvaluateModuloInt(string value, int actual)
    {
        string[] parts = value.Split(':');
        if (parts.Length != 3) return false;

        if (!int.TryParse(parts[1].Trim(), out int divisor) || divisor == 0) return false;
        if (!int.TryParse(parts[2].Trim(), out int remainder)) return false;

        return actual % divisor == remainder;
    }

    /// <summary>
    /// Evaluates modulo for floats. Value format: "VarName:Divisor:Remainder".
    /// Returns true if actual % divisor ≈ remainder (within epsilon).
    /// </summary>
    private static bool EvaluateModuloFloat(string value, float actual)
    {
        string[] parts = value.Split(':');
        if (parts.Length != 3) return false;

        if (!float.TryParse(parts[1].Trim(), out float divisor) || Math.Abs(divisor) < 0.0001f) return false;
        if (!float.TryParse(parts[2].Trim(), out float remainder)) return false;

        float result = actual % divisor;
        return Math.Abs(result - remainder) < 0.0001f;
    }
}
