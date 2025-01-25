﻿using System.Numerics;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NLog.Fluent;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Systems.Trap;

[ServiceBinding(typeof(ShockPylonTrap))]
public class ShockPylonTrap
{
    private const string MeatZapper = "meatzapper";
    private readonly Dictionary<NwArea, List<NwPlaceable>> _activeTraps = new();

    public ShockPylonTrap()
    {
        List<NwPlaceable> traps = NwObject.FindObjectsWithTag<NwPlaceable>(MeatZapper).ToList();

        foreach (NwPlaceable trap in traps)
        {
            if (trap.Area != null && !_activeTraps.ContainsKey(trap.Area))
            {
                _activeTraps.Add(trap.Area, new List<NwPlaceable>());
            }

            if (trap.Area != null) _activeTraps[trap.Area].Add(trap);

            trap.OnDeath += OnTrapDeath;
            trap.Area.OnHeartbeat += Zap;
        }

        NwModule.Instance.OnDMSpawnObject += OnTrapSpawn;
    }

    private void Zap(AreaEvents.OnHeartbeat obj)
    {
        RegisterNewTraps(obj.Area);
        // Don't do anything here if the area's traps are empty
        if (!_activeTraps.TryGetValue(obj.Area, out List<NwPlaceable>? trap))
        {
            return;
        }

        if (trap.Count == 0)
        {
            return;
        }

        NwPlaceable? previous =
            obj.Area.FindObjectsOfTypeInArea<NwPlaceable>().FirstOrDefault(p => p.ResRef == MeatZapper);
        if (previous == null)
        {
            return;
        }


        // Start off by zapping the creature closest to the trap (10m)
        NwCreature? initialClosest = obj.Area.FindObjectsOfTypeInArea<NwCreature>()
            .Where(c => c.Distance(previous) <= 10.0f).OrderBy(c => c.Distance(previous)).FirstOrDefault();

        previous.Location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.DurDeathArmor));

        if (initialClosest != null && initialClosest.IsPlayerControlled)
        {
            Effect initialBeam = NWScript.EffectBeam(NWScript.VFX_BEAM_LIGHTNING, previous, NWScript.BODY_NODE_CHEST,
                0, 2.5f, new Vector3(0, 0, 3))!;
            initialClosest.ApplyEffect(EffectDuration.Temporary, initialBeam, TimeSpan.FromSeconds(2));
            initialClosest.PlaySound("sff_deatharmor");

            int damage = NWScript.d10(4);
            initialClosest.ApplyEffect(EffectDuration.Instant,
                NWScript.EffectDamage(damage, NWScript.DAMAGE_TYPE_ELECTRICAL)!);
            initialClosest.ApplyEffect(EffectDuration.Instant,
                NWScript.EffectDamage(damage, NWScript.DAMAGE_TYPE_NEGATIVE)!);
        }


        foreach (NwPlaceable zapper in _activeTraps[obj.Area].Where(z => z.Distance(previous) <= 30.0f))
        {
            if (zapper == previous)
                continue;

            ApplyBeamEffects(previous, zapper);

            previous = zapper;
        }
    }

    private static void ApplyBeamEffects(NwPlaceable origin, NwPlaceable target)
    {
        Effect beam = NWScript.EffectBeam(NWScript.VFX_BEAM_LIGHTNING, origin, NWScript.BODY_NODE_CHEST, 0,
            2.5f, new Vector3(0, 0, 3))!;
        target.ApplyEffect(EffectDuration.Temporary, beam, TimeSpan.FromSeconds(2));
        target.PlaySound("sff_deatharmor");

        target.Location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.DurDeathArmor));

        // Get the closest creature to the zapper
        NwCreature? closestCreature = target.Area?.FindObjectsOfTypeInArea<NwCreature>()
            .Where(c => c.Distance(target) <= 10.0f).OrderBy(c => c.Distance(target)).FirstOrDefault();

        // If it's a player, apply the effect
        if (closestCreature != null && closestCreature.IsPlayerControlled)
        {
            Effect creatureBeam = NWScript.EffectBeam(NWScript.VFX_BEAM_LIGHTNING, target,
                NWScript.BODY_NODE_CHEST,
                0, 2.5f)!;
            closestCreature.ApplyEffect(EffectDuration.Temporary, creatureBeam, TimeSpan.FromSeconds(2));
            int damage = NWScript.d6(2);
            closestCreature.ApplyEffect(EffectDuration.Instant,
                NWScript.EffectDamage(damage, NWScript.DAMAGE_TYPE_ELECTRICAL)!);
            closestCreature.ApplyEffect(EffectDuration.Instant,
                NWScript.EffectDamage(damage, NWScript.DAMAGE_TYPE_NEGATIVE)!);
        }
    }

    private void OnTrapSpawn(OnDMSpawnObject obj)
    {
        if (obj.ResRef != MeatZapper)
        {
            return;
        }

        if (!_activeTraps.ContainsKey(obj.Area))
        {
            _activeTraps.Add(obj.Area, new List<NwPlaceable>());
            obj.Area.OnHeartbeat += Zap;
        }

        RegisterNewTraps(obj.Area);
    }

    private void RegisterNewTraps(NwArea area)
    {
        // We just want to get the meat zappers that are in the area, but ignore the ones we already have and add them
        // with the rest of the traps
        List<NwPlaceable> traps = area.FindObjectsOfTypeInArea<NwPlaceable>().Where(t => t.Tag == MeatZapper).ToList();
        foreach (NwPlaceable trap in traps)
        {
            if (trap.Area == null) continue;

            if (_activeTraps.ContainsKey(trap.Area) && _activeTraps[trap.Area].Contains(trap))
            {
                continue;
            }

            _activeTraps[trap.Area].Add(trap);
            trap.OnDeath += OnTrapDeath;
        }
    }


    private void OnTrapDeath(PlaceableEvents.OnDeath obj)
    {
        if (obj.KilledObject.ResRef != MeatZapper)
        {
            return;
        }

        if (obj.KilledObject.Area != null &&
            _activeTraps.TryGetValue(obj.KilledObject.Area, out List<NwPlaceable>? trap))
        {
            trap.Remove(obj.KilledObject);
        }
    }
}