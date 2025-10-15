using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Module;

public interface IBlueprint
{
    public string FullName { get; }

    public string Name { get; }

    public string Category { get; }

    public float? ChallengeRating { get; }

    public string Faction { get; }

    public BlueprintObjectType ObjectType { get; }

    public NwObject? Create(Location location);

    public NwItem? Create(NwGameObject owner);
}
