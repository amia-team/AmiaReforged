using AmiaReforged.PwEngine.Features.Encounters.Models;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Encounters.Services;

/// <summary>
/// The core spawning engine for the dynamic encounter system.
/// Takes a trigger, player context, and <see cref="SpawnProfile"/>, then:
///   1. Builds an <see cref="EncounterContext"/>
///   2. Selects an eligible <see cref="SpawnGroup"/> via weighted random
///   3. Spawns creatures at random waypoints within the trigger
///   4. Applies profile-level bonuses to all creatures
///   5. Spawns a mini-boss if configured and the roll succeeds
/// </summary>
[ServiceBinding(typeof(DynamicCreatureSpawner))]
public class DynamicCreatureSpawner
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly Random Rng = new();
    private const string SpawnWaypointTag = "ds_spwn";

    private readonly SpawnGroupSelector _groupSelector;
    private readonly SpawnBonusApplicator _bonusApplicator;
    private readonly MutationApplicator _mutationApplicator;

    public DynamicCreatureSpawner(
        SpawnGroupSelector groupSelector,
        SpawnBonusApplicator bonusApplicator,
        MutationApplicator mutationApplicator)
    {
        _groupSelector = groupSelector;
        _bonusApplicator = bonusApplicator;
        _mutationApplicator = mutationApplicator;
    }

    /// <summary>
    /// Executes the dynamic encounter spawn for a trigger with the given profile and context.
    /// </summary>
    public void SpawnEncounter(
        NwTrigger trigger,
        SpawnProfile profile,
        EncounterContext context)
    {
        IntPtr spawnLocation = GetRandomSpawnLocation(trigger);
        if (spawnLocation == IntPtr.Zero)
        {
            Log.Warn("No spawn waypoints found in trigger for profile '{Name}' (area {Area}).",
                profile.Name, profile.AreaResRef);
            return;
        }

        // Select a group
        SpawnGroup? group = _groupSelector.SelectGroup(profile, context);
        if (group == null)
        {
            Log.Debug("No eligible spawn group selected for profile '{Name}'. Skipping dynamic spawn.",
                profile.Name);
            return;
        }

        // Calculate spawn count, scaled by chaos density
        int baseCount = CalculateBaseSpawnCount(group);
        int scaledCount = ScaleByDensity(baseCount, context.Chaos.Density);
        if (context.PartySize > 6) scaledCount *= 2; // Double spawns for large parties

        Log.Info("Dynamic spawn: profile='{Name}', group='{GroupName}', count={Count} (base={Base}), area={Area}",
            profile.Name, group.Name, scaledCount, baseCount, profile.AreaResRef);

        // Spawn creatures
        List<uint> spawned = SpawnCreaturesFromGroup(group, scaledCount, spawnLocation, profile.DespawnSeconds);

        // Apply profile bonuses and attempt mutation
        IReadOnlyList<SpawnBonus> activeBonuses = profile.Bonuses.Where(b => b.IsActive).ToList();
        foreach (uint creature in spawned)
        {
            _bonusApplicator.ApplyBonuses(creature, activeBonuses, context.Chaos);
            _mutationApplicator.TryApplyMutation(creature, context.Chaos);
        }

        // Mini-boss
        if (profile.MiniBoss != null)
        {
            TrySpawnMiniBoss(profile.MiniBoss, spawnLocation, profile.DespawnSeconds, context.Chaos);
        }
    }

    /// <summary>
    /// Finds a random "ds_spwn" waypoint inside the trigger and returns its location.
    /// </summary>
    private static IntPtr GetRandomSpawnLocation(NwTrigger trigger)
    {
        List<uint> spawnPoints = [];

        uint waypoint = NWScript.GetFirstInPersistentObject(trigger, NWScript.OBJECT_TYPE_WAYPOINT);
        while (waypoint != NWScript.OBJECT_INVALID)
        {
            if (NWScript.GetTag(waypoint) == SpawnWaypointTag)
                spawnPoints.Add(waypoint);
            waypoint = NWScript.GetNextInPersistentObject(trigger, NWScript.OBJECT_TYPE_WAYPOINT);
        }

        if (spawnPoints.Count == 0)
        {
            // Fallback: nearest waypoint with the tag
            uint nearest = NWScript.GetNearestObjectByTag(SpawnWaypointTag, trigger);
            return nearest != NWScript.OBJECT_INVALID ? NWScript.GetLocation(nearest) : IntPtr.Zero;
        }

        return NWScript.GetLocation(spawnPoints[Rng.Next(spawnPoints.Count)]);
    }

    /// <summary>
    /// Calculates the base spawn count by summing weighted random counts from group entries.
    /// </summary>
    private static int CalculateBaseSpawnCount(SpawnGroup group)
    {
        if (group.Entries.Count == 0) return 0;

        // Use the average min/max across entries, similar to legacy d4()+2 (3-6 range)
        int totalMin = group.Entries.Sum(e => e.MinCount);
        int totalMax = group.Entries.Sum(e => e.MaxCount);

        // Roll between aggregate min and max
        return Rng.Next(Math.Min(totalMin, totalMax), Math.Max(totalMin, totalMax) + 1);
    }

    /// <summary>
    /// Scales spawn count by the chaos Density axis.
    /// Density 0 = 1.0x, Density 100 = 1.5x.
    /// </summary>
    private static int ScaleByDensity(int count, int density)
    {
        double scale = 1.0 + density / 200.0;
        return Math.Max(1, (int)Math.Round(count * scale));
    }

    /// <summary>
    /// Spawns creatures from the selected group using weighted random entry selection.
    /// </summary>
    private static List<uint> SpawnCreaturesFromGroup(
        SpawnGroup group,
        int count,
        IntPtr spawnLocation,
        int despawnSeconds)
    {
        List<uint> spawned = [];
        if (group.Entries.Count == 0) return spawned;

        // Build weighted list
        int totalWeight = group.Entries.Sum(e => Math.Max(e.RelativeWeight, 1));

        for (int i = 0; i < count; i++)
        {
            SpawnEntry entry = WeightedSelectEntry(group.Entries, totalWeight);

            uint creature = NWScript.CreateObject(
                NWScript.OBJECT_TYPE_CREATURE,
                entry.CreatureResRef,
                spawnLocation);

            if (creature == NWScript.OBJECT_INVALID)
            {
                Log.Warn("Failed to spawn creature with resref '{ResRef}' — returned OBJECT_INVALID.",
                    entry.CreatureResRef);
                continue;
            }

            NWScript.ChangeToStandardFaction(creature, NWScript.STANDARD_FACTION_HOSTILE);
            NWScript.DestroyObject(creature, despawnSeconds);
            spawned.Add(creature);
        }

        return spawned;
    }

    private static SpawnEntry WeightedSelectEntry(List<SpawnEntry> entries, int totalWeight)
    {
        int roll = Rng.Next(totalWeight);
        int cumulative = 0;

        foreach (SpawnEntry entry in entries)
        {
            cumulative += Math.Max(entry.RelativeWeight, 1);
            if (roll < cumulative)
                return entry;
        }

        return entries[^1];
    }

    /// <summary>
    /// Attempts to spawn a mini-boss based on the configured chance percentage.
    /// </summary>
    private void TrySpawnMiniBoss(
        MiniBossConfig config,
        IntPtr spawnLocation,
        int despawnSeconds,
        ChaosState chaos)
    {
        int roll = Rng.Next(100) + 1;
        if (roll > config.SpawnChancePercent) return;

        uint boss = NWScript.CreateObject(
            NWScript.OBJECT_TYPE_CREATURE,
            config.CreatureResRef,
            spawnLocation);

        if (boss == NWScript.OBJECT_INVALID)
        {
            Log.Warn("Failed to spawn mini-boss with resref '{ResRef}'.", config.CreatureResRef);
            return;
        }

        NWScript.ChangeToStandardFaction(boss, NWScript.STANDARD_FACTION_HOSTILE);
        NWScript.DestroyObject(boss, despawnSeconds);

        // Apply mini-boss-specific bonuses
        IReadOnlyList<SpawnBonus> bossBonuses = config.Bonuses.Where(b => b.IsActive).ToList();
        _bonusApplicator.ApplyBonuses(boss, bossBonuses, chaos);

        Log.Info("Mini-boss spawned: resref='{ResRef}', name='{Name}'.",
            config.CreatureResRef, NWScript.GetName(boss));
    }
}
