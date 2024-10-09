using AmiaReforged.System.Encounters.Types;
using Anvil.API;
using Npgsql.PostgresTypes;
using NWN.Core;

namespace AmiaReforged.System.Encounters;

public class DayNightEncounterSpawner : IEncounterSpawner
{
    private const string DsSpwn = "ds_spwn";
    private static IntPtr _spawnLocation;

    private static readonly string[] VarPrefixes = { "day_spawn", "night_spawn" };
    private static readonly string MiniBossPrefix = "mini_boss";
    private static readonly string MiniBossSpawnChance = "mini_boss_%";
    private static readonly int RandomSizeRange = 15;
    private static readonly int AddonStatusSpawnChance = 2; 
    private static NwArea? _area;
    private readonly NwTrigger _trigger;

    public DayNightEncounterSpawner(NwTrigger trigger)
    {
        _trigger = trigger;
        _area = trigger.Area;
    }

    public bool IsDoubleSpawn { get; set; }

    public void SpawnEncounters()
    {
        SetSpawnPointToRandomWaypointWithinTrigger();

        string[] creatureResRefs = GetResRefsForPrefix(DayNightPrefix()) as string[] ??
                                   GetResRefsForPrefix(DayNightPrefix()).ToArray();

        int numToSpawn = NWScript.d4() + 2;

        int maxSpawns = IsDoubleSpawn ? numToSpawn * 2 : numToSpawn;

        SpawnCreaturesFromResRefs(maxSpawns, creatureResRefs);

        // Spawns in a singular mini boss in the area based on the % chance
        if((NWScript.Random(100)+1) <= NWScript.GetLocalInt(_area,MiniBossSpawnChance))
        SpawnEncounterAtWaypoint(NWScript.GetLocalString(_area,MiniBossPrefix));
    }

    private static string DayNightPrefix() => IsNightTime() && DoSpawnsVary() ? VarPrefixes[1] : VarPrefixes[0];


    private static bool DoSpawnsVary() => NWScript.GetLocalInt(_area, "spawns_vary") == 1;

    private static bool IsNightTime() => NWScript.GetTimeHour() < 6 || NWScript.GetTimeHour() >= 18;

    private void SetSpawnPointToRandomWaypointWithinTrigger()
    {
        uint[] spawnPoints = GetNumberOfSpawnPointsInTrigger() as uint[] ??
                             GetNumberOfSpawnPointsInTrigger().ToArray();

        _spawnLocation = NWScript.GetLocation(GetRandomSpawnPointFromCollection(spawnPoints));
    }

    private uint GetRandomSpawnPointFromCollection(IReadOnlyList<uint> spawnPoints) => spawnPoints.Count == 0
        ? NWScript.GetNearestObjectByTag(DsSpwn, _trigger)
        : spawnPoints[new Random().Next(0, spawnPoints.Count)];

    private IEnumerable<uint> GetNumberOfSpawnPointsInTrigger()
    {
        uint waypoint = NWScript.GetFirstInPersistentObject(_trigger, NWScript.OBJECT_TYPE_WAYPOINT);
        List<uint> spawnPointsInTrigger = new();

        while (waypoint != NWScript.OBJECT_INVALID)
        {
            if (NWScript.GetTag(waypoint).Equals(DsSpwn)) spawnPointsInTrigger.Add(waypoint);
            waypoint = NWScript.GetNextInPersistentObject(_trigger, NWScript.OBJECT_TYPE_WAYPOINT);
        }

        return spawnPointsInTrigger;
    }

    private static IEnumerable<string> GetResRefsForPrefix(string prefix)
    {
        int numberOfLocalVars = _area!.LocalVariables.Count();

        if (numberOfLocalVars == 0)
        {
            return new List<string>();
        }

        IEnumerable<ObjectVariable> spawnVariables = _area.LocalVariables.Where(v => v.Name.Contains(prefix));

        return spawnVariables.Select(var => NWScript.GetLocalString(_area, var.Name)).ToList();
    }

    private static void SpawnCreaturesFromResRefs(int maxSpawns, IReadOnlyList<string> resRefs)
    {
        if (!resRefs.Any())
        {
            return;
        }

        for (int i = 0; i < maxSpawns; i++)
        {
            int randomCreature = new Random().Next(0, resRefs.Count);
            SpawnEncounterAtWaypoint(resRefs[randomCreature]);
        }
    }

    private static void SpawnEncounterAtWaypoint(string resRef)
    {
        if (resRef.Equals(""))
        {
            return;
        }

        uint creature = NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, resRef, _spawnLocation);

        NWScript.DestroyObject(creature, 600.0f);


        // Chance to spawn in a unit with a unique status
        if((NWScript.Random(100)+1) <= AddonStatusSpawnChance)
        {
         ApplyAddonStatus(creature);
        }
        else
        {
         NWScript.SetObjectVisualTransform(creature,10,RandomSizeFloat());
        }

        NWScript.ChangeToStandardFaction(creature, NWScript.STANDARD_FACTION_HOSTILE);

        if (creature == NWScript.OBJECT_INVALID)
            NWScript.WriteTimestampedLogEntry(
                $"Spawn wasn't valid: {resRef} not valid and creature returned OBJECT_INVALID");
    }
    private static float RandomSizeFloat()
    {

      float sizeRange = NWScript.IntToFloat(NWScript.Random(RandomSizeRange)+1);
      float size = 100.0f; 

      if(NWScript.Random(10)<=4)
      {
        size = size + sizeRange; 
      }
      else
      {
        size = size - sizeRange; 
      }
      return size/100;
    }
    private static void ApplyAddonStatus(uint creature)
    {
        int nRandom = NWScript.Random(4);
        switch(nRandom)
        {
            case 0: ApplyGreaterStatus(creature); break;
            case 1: ApplyCageyStatus(creature);  break;
            case 2: ApplyRetributionStatus(creature);  break;
            case 3: ApplyGhostlyStatus(creature);  break;
            default: ApplyGreaterStatus(creature);  break;
        }
    }
    private static void ApplyGreaterStatus(uint creature)
    {
        int level = NWScript.GetLevelByPosition(1,creature) + NWScript.GetLevelByPosition(2,creature) + NWScript.GetLevelByPosition(3,creature); 
        IntPtr eTempHP = NWScript.EffectTemporaryHitpoints(level * 3);
        IntPtr eVisual = NWScript.EffectVisualEffect(411);
        string sName = NWScript.GetName(creature);
        sName = "Greater " + sName; 

        NWScript.SetName(creature,sName);
        NWScript.SetLocalInt(creature,"CustDropPercent",100);
        NWScript.SetObjectVisualTransform(creature,10,1.3f);
        NWScript.ApplyEffectToObject(2,eTempHP,creature);
        NWScript.ApplyEffectToObject(2,eVisual,creature);
    }
    private static void ApplyCageyStatus(uint creature)
    {
        int level = NWScript.GetLevelByPosition(1,creature) + NWScript.GetLevelByPosition(2,creature) + NWScript.GetLevelByPosition(3,creature);
        if(level < 3)
        {
         level = 3;
        }
        IntPtr eAC = NWScript.EffectACIncrease(level/3,0);
        IntPtr eVisual = NWScript.EffectVisualEffect(422);
        string sName = NWScript.GetName(creature);
        sName = "Cagey " + sName; 

        NWScript.SetName(creature,sName);
        NWScript.SetLocalInt(creature,"CustDropPercent",100);
        NWScript.SetObjectVisualTransform(creature,10,0.75f);
        NWScript.ApplyEffectToObject(2,eAC,creature);
        NWScript.ApplyEffectToObject(2,eVisual,creature);
    }
    private static void ApplyRetributionStatus(uint creature)
    {
        int level = NWScript.GetLevelByPosition(1,creature) + NWScript.GetLevelByPosition(2,creature) + NWScript.GetLevelByPosition(3,creature);
        if(level < 5)
        {
         level = 5;
        }
        IntPtr eTempHP = NWScript.EffectTemporaryHitpoints(level);
        IntPtr eVisual = NWScript.EffectVisualEffect(415);
        IntPtr eDamShield = NWScript.EffectDamageShield(level/5,1,1);
        string sName = NWScript.GetName(creature);
        sName = "Retribution " + sName; 

        NWScript.SetName(creature,sName);
        NWScript.SetLocalInt(creature,"CustDropPercent",100);
        NWScript.SetObjectVisualTransform(creature,10,1.2f);
        NWScript.ApplyEffectToObject(2,eTempHP,creature);
        NWScript.ApplyEffectToObject(2,eVisual,creature);
        NWScript.ApplyEffectToObject(2,eDamShield,creature);
    }
     private static void ApplyGhostlyStatus(uint creature)
    {
        int level = NWScript.GetLevelByPosition(1,creature) + NWScript.GetLevelByPosition(2,creature) + NWScript.GetLevelByPosition(3,creature);
        if(level >= 30)
        {
            level = 30;
        }
        IntPtr eConceal = NWScript.EffectConcealment(level*2);
        IntPtr eVisual = NWScript.EffectVisualEffect(9);
        string sName = NWScript.GetName(creature);
        sName = "Ghostly " + sName; 

        NWScript.SetName(creature,sName);
        NWScript.SetLocalInt(creature,"CustDropPercent",100);
        NWScript.ApplyEffectToObject(2,eConceal,creature);
        NWScript.ApplyEffectToObject(2,eVisual,creature);
    }
}
