namespace AmiaReforged.PwEngine.Features.Encounters.Models;

/// <summary>
/// Types of conditions that control when a spawn group is eligible to activate.
/// </summary>
public enum SpawnConditionType
{
    /// <summary>
    /// Checks game time-of-day against a time window (e.g., "06:00-18:00").
    /// </summary>
    TimeOfDay = 0,

    /// <summary>
    /// Checks a chaos axis against a threshold (e.g., "Danger>=50").
    /// </summary>
    ChaosThreshold = 1,

    /// <summary>
    /// Minimum number of players in the party.
    /// </summary>
    MinPlayerCount = 2,

    /// <summary>
    /// Maximum number of players in the party.
    /// </summary>
    MaxPlayerCount = 3,

    /// <summary>
    /// Matches against a region tag.
    /// </summary>
    RegionTag = 4,

    /// <summary>
    /// Matches the spawn trigger's tag. Operators: ==, !=, contains, startswith.
    /// Value is the string to compare against.
    /// </summary>
    TriggerTag = 5,

    /// <summary>
    /// Checks a local integer variable on the spawn trigger.
    /// Operators: ==, !=, &gt;, &gt;=, &lt;, &lt;=, %.
    /// Value format: "VariableName:CompareValue".
    /// For % operator: "VariableName:Divisor:Remainder" (e.g., "counter:3:0" means var % 3 == 0).
    /// </summary>
    TriggerLocalVariableInt = 10,

    /// <summary>
    /// Checks a local float variable on the spawn trigger.
    /// Operators: ==, !=, &gt;, &gt;=, &lt;, &lt;=, %.
    /// Value format: "VariableName:CompareValue".
    /// For % operator: "VariableName:Divisor:Remainder".
    /// </summary>
    TriggerLocalVariableFloat = 11,

    /// <summary>
    /// Checks a local string variable on the spawn trigger.
    /// Operators: ==, !=, contains, startswith.
    /// Value format: "VariableName:CompareValue".
    /// </summary>
    TriggerLocalVariableString = 12,

    /// <summary>
    /// Checks a local integer variable on the area.
    /// Operators: ==, !=, &gt;, &gt;=, &lt;, &lt;=, %.
    /// Value format: "VariableName:CompareValue".
    /// For % operator: "VariableName:Divisor:Remainder".
    /// </summary>
    AreaLocalVariableInt = 20,

    /// <summary>
    /// Checks a local float variable on the area.
    /// Operators: ==, !=, &gt;, &gt;=, &lt;, &lt;=, %.
    /// Value format: "VariableName:CompareValue".
    /// For % operator: "VariableName:Divisor:Remainder".
    /// </summary>
    AreaLocalVariableFloat = 21,

    /// <summary>
    /// Checks a local string variable on the area.
    /// Operators: ==, !=, contains, startswith.
    /// Value format: "VariableName:CompareValue".
    /// </summary>
    AreaLocalVariableString = 22,

    /// <summary>
    /// Custom condition for future extensibility. Always evaluates to true.
    /// </summary>
    Custom = 99
}
