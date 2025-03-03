using System.Numerics;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.Trap;

[ServiceBinding(typeof(PillarOfPhantoms))]
public class PillarOfPhantoms
{
    private const string PillarOfPhantomsTag = "ghostpillar";
    private const string PillarPhantomTag = "pillarphantom";
    private readonly Dictionary<NwArea, List<NwPlaceable>> _activeTraps = new();

    public PillarOfPhantoms()
    {
        List<NwPlaceable> traps = NwObject.FindObjectsWithTag<NwPlaceable>(PillarOfPhantomsTag).ToList();
        foreach (NwPlaceable trap in traps)
        {
            if (trap.Area != null && !_activeTraps.ContainsKey(trap.Area))
            {
                _activeTraps.Add(trap.Area, new List<NwPlaceable>());
            }

            if (trap.Area != null) _activeTraps[trap.Area].Add(trap);

            trap.OnHeartbeat += DoSpawnRoutine;
            trap.OnDeath += OnTrapDeath;
        }

        NwModule.Instance.OnDMSpawnObject += OnTrapSpawn;
    }

    private void OnTrapSpawn(OnDMSpawnObject obj)
    {
        if (obj.ObjectType != ObjectTypes.Placeable) return;
        if (obj.ResRef != PillarOfPhantomsTag) return;
       
        if (!_activeTraps.ContainsKey(obj.Area))
        {
            _activeTraps.Add(obj.Area, new List<NwPlaceable>());
        }

        RegisterNewTraps(obj.Area);
    }

    private void RegisterNewTraps(NwArea area)
    {
        List<NwPlaceable> traps = area.FindObjectsOfTypeInArea<NwPlaceable>()
            .Where(t => t.Tag == PillarOfPhantomsTag && !_activeTraps[area].Contains(t)).ToList();

        foreach (NwPlaceable trap in traps)
        {
            _activeTraps[area].Add(trap);
            trap.OnHeartbeat += DoSpawnRoutine;
            trap.OnDeath += OnTrapDeath;
        }
    }

    private void DoSpawnRoutine(PlaceableEvents.OnHeartbeat obj)
    {
        if (obj.Placeable.Tag != PillarOfPhantomsTag) return;


        // Get creatures that are player controlled within 25 meters of the trap
        List<NwCreature> creatures = obj.Placeable.Area.FindObjectsOfTypeInArea<NwCreature>()
            .Where(c => c.IsPlayerControlled && c.Distance(obj.Placeable) <= 10f).ToList();

        foreach (NwCreature unused in creatures)
        {
            // Pick a random, valid location within 4 meters of the trap
            int randomXOffset = Random.Shared.Next(-4, 4);
            int randomYOffset = Random.Shared.Next(-4, 4);
            // The Z axis is shared with the trap, so we don't need to randomize it
            Location spawnLocation = Location.Create(obj.Placeable.Area,
                new Vector3(obj.Placeable.Position.X + randomXOffset, obj.Placeable.Position.Y + randomYOffset, obj.Placeable.Position.Z),
                obj.Placeable.Rotation);

            // Is it safe to spawn the phantom here?
            if (spawnLocation.IsWalkable)
            {
                // Just to make sure, we're going to set the spawn location's Z value to its walkable height
                Location trueSpawnLocation = Location.Create(obj.Placeable.Area,
                    new Vector3(spawnLocation.Position.X, spawnLocation.Position.Y, spawnLocation.GroundHeight),
                    obj.Placeable.Rotation);
                NwCreature.Create(PillarPhantomTag, trueSpawnLocation);
                Effect spawnVfx =
                    Effect.VisualEffect(VfxType.FnfSummonUndead, false, Random.Shared.NextFloat(-1.5f, 1.5f));
                Effect secondaryVfx =
                    Effect.VisualEffect(VfxType.ImpRaiseDead, false, Random.Shared.NextFloat(-1.5f, 1.5f));

                // Apply at location
                trueSpawnLocation.ApplyEffect(EffectDuration.Instant, spawnVfx);
                trueSpawnLocation.ApplyEffect(EffectDuration.Instant, secondaryVfx);
            }
        }
    }

    private void OnTrapDeath(PlaceableEvents.OnDeath obj)
    {
        if (obj.KilledObject.Tag != PillarOfPhantomsTag) return;
        if (obj.KilledObject.Area == null) return;
        if (!_activeTraps.ContainsKey(obj.KilledObject.Area)) return;

        _activeTraps[obj.KilledObject.Area].Remove(obj.KilledObject);
    }
}