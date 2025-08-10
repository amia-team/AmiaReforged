namespace AmiaReforged.PwEngine.Systems.WorldEngine.Definitions;

public class Industry
{

    /// <summary>
    /// Display name for the user. Used in UIs.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Uniquely identifies an industry within the economy system
    /// </summary>
    public required string Tag { get; set; }

    /// <summary>
    /// Display string for UIs. Gives the user context when requested.
    /// </summary>
    public required string Description { get; set; }

    public List<Knowledge>? Knowledge { get; set; }
}

public class Field
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string ParentIndustry { get; set; }
    public List<Knowledge>? Knowledge { get; set; }
}

public class Knowledge
{
    public required string Name { get; set; }
    public required string Tag { get; set; }
    public required string Description { get; set; }
    public FieldRank Rank { get; set; } = FieldRank.None;

    public IReadOnlyCollection<ActionEffect>? Effects { get; set; }

}


public enum FieldRank
{
    None = 0,
    Novice = 1,
    Apprentice = 2,
    Expert = 3,
    Master = 4,
    Grandmaster = 5,
}

public class ActionEffect
{
    /// <summary>
    /// A list of tags to append additional effects to.
    /// </summary>
    public List<string> TagFilters { get; set; } = [];
    /// <summary>
    /// A list of actions that get run in the order they're defined.
    /// </summary>
    public IReadOnlyCollection<WorldActionDefinition>? ModifiesActions { get; set; }
}

public class WorldActionDefinition
{
    public required string Name { get; set; }
    public required float BaseTime { get; set; }
    public List<WorldActionOutput>? Outputs { get; set; }
    public List<WorldEventDefinition>? EventsBefore { get; set; }
    public List<WorldEventDefinition>? EventsSuccess { get; set; }
    public List<WorldEventDefinition>? EventsFail { get; set; }
    public List<WorldEventDefinition>? EventsAfter { get; set; }

}

public class WorldEventDefinition
{
    public required string EventName { get; set; }
}

public class WorldActionOutput
{
}

public class SkillLevel
{
    public string Name { get; set; }
    public string Description { get; set; }
    public SkillLevel? PrecededBy { get; set; }
}
