namespace AmiaReforged.PwEngine.Features.WorldEngine.Traits;

public class Trait
{
    public required string Tag { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required int PointCost { get; init; }
    public bool RequiresUnlock { get; init; }
    public List<string> AllowedRaces { get; init; } = [];
    public List<string> AllowedClasses { get; init; } = [];
    public List<string> ForbiddenRaces { get; init; } = [];
    public List<string> ForbiddenClasses { get; init; } = [];
    public List<string> ConflictingTraits { get; init; } = [];
    public List<string> PrerequisiteTraits { get; init; } = [];
}
