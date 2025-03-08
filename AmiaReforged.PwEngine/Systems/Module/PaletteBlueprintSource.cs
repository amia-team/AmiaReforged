using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.Module;

[ServiceBinding(typeof(IBlueprintSource))]
internal sealed class PaletteBlueprintSource : IBlueprintSource
{
    private const string CreaturePaletteResRef = "creaturepal";
    private const string DoorPaletteResRef = "doorpal";
    private const string EncounterPaletteResRef = "encounterpal";
    private const string ItemPaletteResRef = "itempal";
    private const string PlaceablePaletteResRef = "placeablepal";
    private const string SoundPaletteResRef = "soundpal";
    private const string StorePaletteResRef = "storepal";
    private const string TriggerPaletteResRef = "triggerpal";
    private const string WaypointPaletteResRef = "waypointpal";

    private readonly List<IBlueprint> _blueprints = new List<IBlueprint>();

    public PaletteBlueprintSource(InjectionService injectionService)
    {
        _blueprints.AddRange(injectionService.Inject(new Palette(CreaturePaletteResRef, BlueprintObjectType.Creature))
            .GetBlueprints());
        _blueprints.AddRange(injectionService.Inject(new Palette(DoorPaletteResRef, BlueprintObjectType.Door))
            .GetBlueprints());
        _blueprints.AddRange(injectionService.Inject(new Palette(EncounterPaletteResRef, BlueprintObjectType.Encounter))
            .GetBlueprints());
        _blueprints.AddRange(injectionService.Inject(new Palette(ItemPaletteResRef, BlueprintObjectType.Item))
            .GetBlueprints());
        _blueprints.AddRange(injectionService.Inject(new Palette(PlaceablePaletteResRef, BlueprintObjectType.Placeable))
            .GetBlueprints());
        _blueprints.AddRange(injectionService.Inject(new Palette(SoundPaletteResRef, BlueprintObjectType.Sound))
            .GetBlueprints());
        _blueprints.AddRange(injectionService.Inject(new Palette(StorePaletteResRef, BlueprintObjectType.Store))
            .GetBlueprints());
        _blueprints.AddRange(injectionService.Inject(new Palette(TriggerPaletteResRef, BlueprintObjectType.Trigger))
            .GetBlueprints());
        _blueprints.AddRange(injectionService.Inject(new Palette(WaypointPaletteResRef, BlueprintObjectType.Waypoint))
            .GetBlueprints());
    }

    public IEnumerable<IBlueprint> GetBlueprints(BlueprintObjectType blueprintType, int start, string search, int count)
    {
        return _blueprints.Where(blueprint =>
                blueprint.ObjectType == blueprintType &&
                blueprint.FullName.Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(blueprint => blueprint.FullName).Skip(start).Take(count);
    }
}