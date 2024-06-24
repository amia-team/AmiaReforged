using AmiaReforged.System.Encounters.Types;
using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.Encounters;

public class DayNightEncounterSpawner : IEncounterSpawner
{
    private const string DsSpwn = "ds_spwn";
    private static IntPtr _spawnLocation;

    private static readonly string[] VarPrefixes = { "day_spawn", "night_spawn" };
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

        NWScript.ChangeToStandardFaction(creature, NWScript.STANDARD_FACTION_HOSTILE);

        if (creature == NWScript.OBJECT_INVALID)
            NWScript.WriteTimestampedLogEntry(
                $"Spawn wasn't valid: {resRef} not valid and creature returned OBJECT_INVALID");
    }
}