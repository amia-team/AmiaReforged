using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Module;

internal sealed class PaletteBlueprint : IBlueprint
{
    public string ResRef { get; init; }
    public string FullName { get; init; }
    public string Name { get; init; }
    public string Category { get; init; }
    public float? ChallengeRating { get; init; }
    public string Faction { get; init; }
    public BlueprintObjectType ObjectType { get; init; }

    public NwObject? Create(Location location)
    {
        return ObjectType switch
        {
            BlueprintObjectType.Creature => NwCreature.Create(ResRef, location),
            BlueprintObjectType.Item => NwItem.Create(ResRef, location),
            BlueprintObjectType.Placeable => NwPlaceable.Create(ResRef, location),
            BlueprintObjectType.Store => NwStore.Create(ResRef, location),
            BlueprintObjectType.Waypoint => NwWaypoint.Create(ResRef, location),
            BlueprintObjectType.Door => NwDoor.Create(ResRef, location),
            BlueprintObjectType.Encounter => NwEncounter.Create(ResRef, location),
            BlueprintObjectType.Sound => NwSound.Create(ResRef, location),
            BlueprintObjectType.Trigger => NwTrigger.Create(ResRef, location),
            _ => throw new NotImplementedException($"{ObjectType} blueprints are not supported."),
        };
    }

    public NwItem? Create(NwGameObject owner)
    {
        if (ObjectType is not BlueprintObjectType.Item || owner == null)
        {
            return null;
        }

        NwItem? item = null;
        if (owner is NwCreature creature)
        {
            item = NwItem.Create(ResRef, owner.Location!);
            if (item != null)
            {
                creature.AcquireItem(item);
            }
        }
        else if (owner is NwPlaceable placeable && placeable.HasInventory)
        {
            item = NwItem.Create(ResRef, owner.Location!);
            if (item != null)
            {
                placeable.AcquireItem(item);
            }
        }

        return item;
    }
}