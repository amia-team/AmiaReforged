﻿using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NLog.Fluent;

namespace AmiaReforged.PwEngine.Systems.Trap;

[ServiceBinding(typeof(ShockPylonTrap))]
public class ShockPylonTrap
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
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
            
            trap.OnHeartbeat += Zap;
            trap.OnDeath += OnTrapDeath;
        }

        NwModule.Instance.OnDMSpawnObject += OnTrapSpawn;
    }

    private void OnTrapSpawn(OnDMSpawnObject obj)
    {
        if (obj.ResRef != MeatZapper)
        {
            return;
        }

        NwPlaceable? trap = obj.SpawnedObject as NwPlaceable;
        if (trap == null)
        {
            Log.Info("Trap is not a placeable.");
            return;
        }

        if (!_activeTraps.ContainsKey(obj.Area))
        {
            _activeTraps.Add(obj.Area, new List<NwPlaceable>());
        }

        _activeTraps[obj.Area].Add(trap);


        trap.OnHeartbeat += Zap;
        trap.OnDeath += OnTrapDeath;
    }

    private void Zap(PlaceableEvents.OnHeartbeat obj)
    {
        Log.Info("Zap time");
        // Find zappers in a 50 meter radius
        List<NwPlaceable>? zappers = obj.Placeable.Area?.FindObjectsOfTypeInArea<NwPlaceable>()
            .Where(p => p.ResRef == MeatZapper && p.Distance(obj.Placeable) <= 100.0f && p != obj.Placeable).ToList();
        

        if (zappers != null)
        {
            NwPlaceable previous = obj.Placeable;
            foreach (NwPlaceable zapper in zappers)
            {
                Effect beam = Effect.Beam(VfxType.BeamBlack, previous, BodyNode.Chest);
                zapper.ApplyEffect(EffectDuration.Instant, beam, TimeSpan.FromSeconds(2));
                
                previous = zapper;
            }

        }
    }

    private void OnTrapDeath(PlaceableEvents.OnDeath obj)
    {
        if(obj.KilledObject.ResRef != MeatZapper)
        {
            return;
        }
        
        if (obj.KilledObject.Area != null && _activeTraps.TryGetValue(obj.KilledObject.Area, out List<NwPlaceable>? trap))
        {
            trap.Remove(obj.KilledObject);
        }
    }
}