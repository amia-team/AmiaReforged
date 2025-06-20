namespace AmiaReforged.PwEngine.Systems.WorldEngine.Definitions;

public class Industry
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }

    public IReadOnlyCollection<Field> Fields { get; set; } = [];
}

public class Field
{
    public string Name { get; set; }
    public List<Knowledge>? Knowledge { get; set; } 
}

public class Knowledge
{
    public string Name { get; set; }
    public string Description { get; set; }
    public SkillLevel Complexity { get; set; }
    
    public IReadOnlyCollection<ActionEffect>? Effects { get; set; }
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