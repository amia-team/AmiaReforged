using AmiaReforged.PwEngine.Features.Encounters.Models;
using AmiaReforged.PwEngine.Features.Encounters.Services;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Encounters.Migration;

/// <summary>
/// Runs at startup after <see cref="DynamicEncounterService"/> to scan all areas for legacy
/// spawn local variables (day_spawn*, night_spawn*, mini_boss, etc.). For each area that has
/// legacy variables but no existing DB profile, generates a <see cref="SpawnProfile"/> and
/// persists it as <b>inactive</b> — the legacy system continues to operate until a DM
/// explicitly activates the migrated profile.
/// </summary>
[ServiceBinding(typeof(LegacySpawnMigrationService))]
public class LegacySpawnMigrationService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private const string DaySpawnPrefix = "day_spawn";
    private const string NightSpawnPrefix = "night_spawn";
    private const string MiniBossVar = "mini_boss";
    private const string MiniBossChanceVar = "mini_boss_%";

    private readonly ISpawnProfileRepository _repository;

    public LegacySpawnMigrationService(ISpawnProfileRepository repository)
    {
        _repository = repository;
        RunMigration().GetAwaiter().GetResult();
    }

    private async Task RunMigration()
    {
        Log.Info("Legacy spawn migration: scanning areas for local variable spawn definitions...");

        int migratedCount = 0;
        int skippedCount = 0;

        foreach (NwArea area in NwModule.Instance.Areas)
        {
            string areaResRef = area.ResRef;

            // Skip areas that already have a profile in the DB
            if (await _repository.ExistsForAreaAsync(areaResRef))
            {
                skippedCount++;
                continue;
            }

            // Collect legacy spawn resrefs
            List<string> dayResRefs = GetResRefsForPrefix(area, DaySpawnPrefix);
            List<string> nightResRefs = GetResRefsForPrefix(area, NightSpawnPrefix);
            string miniBossResRef = NWScript.GetLocalString(area, MiniBossVar);
            int miniBossChance = NWScript.GetLocalInt(area, MiniBossChanceVar);

            // If no legacy spawns at all, skip
            if (dayResRefs.Count == 0 && nightResRefs.Count == 0 && string.IsNullOrEmpty(miniBossResRef))
                continue;

            // Build profile
            SpawnProfile profile = BuildProfileFromLegacy(
                area, areaResRef, dayResRefs, nightResRefs, miniBossResRef, miniBossChance);

            await _repository.CreateAsync(profile);
            migratedCount++;

            Log.Info("Legacy spawn migration: created profile '{Name}' for area '{ResRef}' " +
                     "({DayCount} day, {NightCount} night spawns, miniboss={HasMiniBoss}).",
                profile.Name, areaResRef, dayResRefs.Count, nightResRefs.Count,
                !string.IsNullOrEmpty(miniBossResRef));
        }

        Log.Info("Legacy spawn migration complete: {Migrated} profiles created, {Skipped} areas already had profiles.",
            migratedCount, skippedCount);
    }

    private static List<string> GetResRefsForPrefix(NwArea area, string prefix)
    {
        List<string> resRefs = [];

        foreach (ObjectVariable variable in area.LocalVariables)
        {
            if (!variable.Name.Contains(prefix, StringComparison.OrdinalIgnoreCase)) continue;

            string value = NWScript.GetLocalString(area, variable.Name);
            if (!string.IsNullOrWhiteSpace(value))
                resRefs.Add(value);
        }

        return resRefs;
    }

    private static SpawnProfile BuildProfileFromLegacy(
        NwArea area,
        string areaResRef,
        List<string> dayResRefs,
        List<string> nightResRefs,
        string miniBossResRef,
        int miniBossChance)
    {
        bool spawnsVary = NWScript.GetLocalInt(area, "spawns_vary") == 1;
        string areaName = NWScript.GetName(area);

        SpawnProfile profile = new()
        {
            Id = Guid.NewGuid(),
            AreaResRef = areaResRef,
            Name = $"[Migrated] {areaName}",
            IsActive = false, // Inactive by default — DM must explicitly activate
            CooldownSeconds = 900,
            DespawnSeconds = 600
        };

        // Day spawns group
        if (dayResRefs.Count > 0)
        {
            SpawnGroup dayGroup = new()
            {
                Id = Guid.NewGuid(),
                Name = "Day Spawns",
                Weight = 1,
                Conditions =
                [
                    new SpawnCondition
                    {
                        Id = Guid.NewGuid(),
                        Type = SpawnConditionType.TimeOfDay,
                        Operator = "between",
                        Value = "06:00-18:00"
                    }
                ],
                Entries = dayResRefs.Select(resRef => new SpawnEntry
                {
                    Id = Guid.NewGuid(),
                    CreatureResRef = resRef,
                    RelativeWeight = 1,
                    MinCount = 1,
                    MaxCount = 4
                }).ToList()
            };
            profile.SpawnGroups.Add(dayGroup);

            // If spawns don't vary, day spawns also serve as night spawns (no night-specific group)
            if (!spawnsVary && nightResRefs.Count == 0)
            {
                // Add an unconditional alias so the day group works at all times
                dayGroup.Conditions.Clear();
            }
        }

        // Night spawns group (only if spawns_vary is set and there are night-specific resrefs)
        if (spawnsVary && nightResRefs.Count > 0)
        {
            SpawnGroup nightGroup = new()
            {
                Id = Guid.NewGuid(),
                Name = "Night Spawns",
                Weight = 1,
                Conditions =
                [
                    new SpawnCondition
                    {
                        Id = Guid.NewGuid(),
                        Type = SpawnConditionType.TimeOfDay,
                        Operator = "between",
                        Value = "18:00-06:00"
                    }
                ],
                Entries = nightResRefs.Select(resRef => new SpawnEntry
                {
                    Id = Guid.NewGuid(),
                    CreatureResRef = resRef,
                    RelativeWeight = 1,
                    MinCount = 1,
                    MaxCount = 4
                }).ToList()
            };
            profile.SpawnGroups.Add(nightGroup);
        }

        // Mini-boss
        if (!string.IsNullOrEmpty(miniBossResRef))
        {
            profile.MiniBoss = new MiniBossConfig
            {
                Id = Guid.NewGuid(),
                CreatureResRef = miniBossResRef,
                SpawnChancePercent = miniBossChance > 0 ? miniBossChance : 5
            };
        }

        return profile;
    }
}
