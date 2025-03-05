using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Trap;

[ServiceBinding(typeof(ShockPylonTrap))]
public class ShockPylonTrap
{
    private const string MeatZapper = "meatzapper";
    private readonly Dictionary<NwArea, List<NwPlaceable>> _activeTraps = new();

    public ShockPylonTrap()
    {
        List<NwPlaceable?> traps = NwObject.FindObjectsWithTag<NwPlaceable>(MeatZapper).ToList();

        foreach (NwPlaceable? trap in traps)
        {
            if (trap.Area != null && !_activeTraps.ContainsKey(trap.Area)) _activeTraps.Add(trap.Area, new());

            if (trap.Area != null) _activeTraps[trap.Area].Add(trap);

            trap.OnHeartbeat += Zap;
            trap.OnDeath += OnTrapDeath;
        }

        NwModule.Instance.OnDMSpawnObject += OnTrapSpawn;
    }

    private static void ApplyBeamEffects(NwPlaceable origin, NwPlaceable target)
    {
        Effect beam = NWScript.EffectBeam(NWScript.VFX_BEAM_LIGHTNING, origin, NWScript.BODY_NODE_CHEST, 0,
            2.5f, new(0, 0, 3))!;
        target.ApplyEffect(EffectDuration.Temporary, beam, TimeSpan.FromSeconds(2));


        // Shock creatures near it in a 10m radius
        List<NwCreature> creatures = target.Area!.FindObjectsOfTypeInArea<NwCreature>()
            .Where(c => c.IsPlayerControlled && c.Distance(target) <= 10f).ToList();

        foreach (NwCreature creature in creatures)
        {
            // beam
            Effect creatureBeam = NWScript.EffectBeam(NWScript.VFX_BEAM_LIGHTNING, origin, NWScript.BODY_NODE_CHEST, 0,
                2.5f, new(0, 0, 3))!;

            creature.ApplyEffect(EffectDuration.Temporary, creatureBeam, TimeSpan.FromSeconds(2));

            // damage
            int damage = NWScript.d6(2);
            creature.ApplyEffect(EffectDuration.Instant,
                NWScript.EffectDamage(damage, NWScript.DAMAGE_TYPE_ELECTRICAL)!);
            creature.ApplyEffect(EffectDuration.Instant, NWScript.EffectDamage(damage, NWScript.DAMAGE_TYPE_NEGATIVE)!);
        }
    }

    private void OnTrapSpawn(OnDMSpawnObject obj)
    {
        if (obj.ResRef != MeatZapper) return;

        if (!_activeTraps.ContainsKey(obj.Area)) _activeTraps.Add(obj.Area, new());

        RegisterNewTraps(obj.Area);
    }

    private void RegisterNewTraps(NwArea area)
    {
        // We just want to get the meat zappers that are in the area, but ignore the ones we already have and add them
        // with the rest of the traps
        List<NwPlaceable?> traps = area.FindObjectsOfTypeInArea<NwPlaceable>().Where(t => t.Tag == MeatZapper).ToList();
        foreach (NwPlaceable? trap in traps)
        {
            if (trap.Area == null) continue;

            if (_activeTraps.ContainsKey(trap.Area) && _activeTraps[trap.Area].Contains(trap)) continue;

            _activeTraps[trap.Area].Add(trap);
            trap.OnHeartbeat += Zap;
            trap.OnDeath += OnTrapDeath;
        }
    }

    private void Zap(PlaceableEvents.OnHeartbeat obj)
    {
        if (obj.Placeable.Tag != MeatZapper) return;


        // Get the closest player controlled creatures within 10m
        List<NwCreature> creatures = obj.Placeable.Area!.FindObjectsOfTypeInArea<NwCreature>()
            .Where(c => c.IsPlayerControlled && c.Distance(obj.Placeable) <= 10f).ToList();

        ApplyDamageAndBeamEffects(obj.Placeable, creatures);

        // Just get the closest zapper within 20m
        NwPlaceable? closestZapper = obj.Placeable.Area!.FindObjectsOfTypeInArea<NwPlaceable>()
            .Where(t => t.Tag == MeatZapper && t != obj.Placeable && t.Distance(obj.Placeable) <= 20f)
            .OrderBy(t => t.Distance(obj.Placeable)).FirstOrDefault();

        if (closestZapper == null)
            return;

        ApplyBeamEffects(obj.Placeable, closestZapper);
    }

    private void ApplyDamageAndBeamEffects(NwPlaceable objPlaceable, List<NwCreature> creatures)
    {
        foreach (NwCreature creature in creatures)
        {
            // beam
            Effect creatureBeam = NWScript.EffectBeam(NWScript.VFX_BEAM_LIGHTNING, objPlaceable,
                NWScript.BODY_NODE_CHEST, 0,
                2.5f, new(0, 0, 3))!;

            creature.ApplyEffect(EffectDuration.Temporary, creatureBeam, TimeSpan.FromSeconds(2));

            // damage
            int damage = NWScript.d6(2);
            creature.ApplyEffect(EffectDuration.Instant,
                NWScript.EffectDamage(damage, NWScript.DAMAGE_TYPE_ELECTRICAL)!);
            creature.ApplyEffect(EffectDuration.Instant, NWScript.EffectDamage(damage, NWScript.DAMAGE_TYPE_NEGATIVE)!);
        }
    }


    private void OnTrapDeath(PlaceableEvents.OnDeath obj)
    {
        if (obj.KilledObject.ResRef != MeatZapper) return;

        if (obj.KilledObject.Area != null &&
            _activeTraps.TryGetValue(obj.KilledObject.Area, out List<NwPlaceable>? trap))
            trap.Remove(obj.KilledObject);
    }
}