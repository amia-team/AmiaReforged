using AmiaReforged.PwEngine.Features.Encounters.Models;
using AmiaReforged.PwEngine.Features.Encounters.Services;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Encounters;

/// <summary>
/// Dynamic encounter service that replaces (or falls back to) the legacy area-local-variable
/// spawning system. On trigger entry:
///   1. If a <see cref="SpawnProfile"/> exists and is active for the trigger's area, the
///      dynamic system handles the encounter and sets a flag so the legacy service skips.
///   2. If no active profile exists, the trigger is left unflagged and the legacy
///      <c>EncounterService</c> in AmiaReforged.System handles it.
/// </summary>
[ServiceBinding(typeof(DynamicEncounterService))]
public class DynamicEncounterService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Local variable set on the trigger when the dynamic system handles the spawn.
    /// The legacy EncounterService checks for this and skips if present.
    /// </summary>
    public const string DynamicHandledFlag = "dynamic_handled";

    private const int CooldownFlagVar = 1; // NWScript.TRUE
    private const string CooldownStartVar = "dyn_cooldown_start";
    private const string CooldownActiveVar = "dyn_on_cooldown";

    /// <summary>
    /// Area-level flag that indicates OnAreaEnter groups have already been spawned.
    /// Prevents re-spawning when subsequent triggers fire in the same area.
    /// Reset when the cooldown expires via <see cref="NWScript.DelayCommand"/>.
    /// </summary>
    private const string AreaEnterSpawnedFlag = "dyn_area_enter_spawned";

    private readonly ISpawnProfileRepository _repository;
    private readonly IRegionSubsystem _regionSubsystem;
    private readonly DynamicCreatureSpawner _spawner;

    /// <summary>
    /// In-memory cache of active profiles keyed by area resref.
    /// Refreshed at startup; profiles activated/deactivated via API update this cache.
    /// </summary>
    private readonly Dictionary<string, SpawnProfile> _profileCache = new(StringComparer.OrdinalIgnoreCase);

    public DynamicEncounterService(
        ISpawnProfileRepository repository,
        IRegionSubsystem regionSubsystem,
        DynamicCreatureSpawner spawner)
    {
        _repository = repository;
        _regionSubsystem = regionSubsystem;
        _spawner = spawner;

        InitializeAsync().GetAwaiter().GetResult();
    }

    private async Task InitializeAsync()
    {
        // Load active profiles into cache
        List<SpawnProfile> activeProfiles = await _repository.GetAllActiveAsync();
        foreach (SpawnProfile profile in activeProfiles)
        {
            _profileCache[profile.AreaResRef] = profile;
        }

        Log.Info("Dynamic encounter service loaded {Count} active profiles.", _profileCache.Count);

        // Subscribe to all spawn triggers
        IEnumerable<NwTrigger> triggers = NwObject.FindObjectsWithTag<NwTrigger>("db_spawntrigger");
        foreach (NwTrigger trigger in triggers)
        {
            trigger.OnEnter += OnTriggerEnter;
        }

        // Subscribe to all boss triggers
        IEnumerable<NwTrigger> bossTriggers = NwObject.FindObjectsWithTag<NwTrigger>("db_bosstrigger");
        foreach (NwTrigger trigger in bossTriggers)
        {
            trigger.OnEnter += OnBossTriggerEnter;
        }

        Log.Info("Dynamic encounter service initialized.");
    }

    /// <summary>
    /// Refreshes the cached profile for a given area. Call after API create/update/delete/activate.
    /// </summary>
    public async Task RefreshProfileCacheAsync(string areaResRef)
    {
        SpawnProfile? profile = await _repository.GetByAreaResRefAsync(areaResRef);
        if (profile is { IsActive: true })
        {
            _profileCache[areaResRef] = profile;
        }
        else
        {
            _profileCache.Remove(areaResRef);
        }
    }

    /// <summary>
    /// Refreshes the entire profile cache. Call after bulk operations.
    /// </summary>
    public async Task RefreshAllProfileCacheAsync()
    {
        _profileCache.Clear();
        List<SpawnProfile> activeProfiles = await _repository.GetAllActiveAsync();
        foreach (SpawnProfile profile in activeProfiles)
        {
            _profileCache[profile.AreaResRef] = profile;
        }

        Log.Info("Dynamic encounter cache refreshed: {Count} active profiles.", _profileCache.Count);
    }

    private void OnTriggerEnter(TriggerEvents.OnEnter obj)
    {
        // Clear any previous dynamic_handled flag at the start
        NWScript.SetLocalInt(obj.Trigger, DynamicHandledFlag, NWScript.FALSE);

        if (!obj.EnteringObject.IsPlayerControlled(out NwPlayer? player)) return;
        if (player.IsDM || player.IsPlayerDM) return;

        NwArea? area = obj.Trigger.Area;
        if (area == null) return;

        string areaResRef = area.ResRef;

        // Check if we have an active dynamic profile for this area
        if (!_profileCache.TryGetValue(areaResRef, out SpawnProfile? profile))
        {
            // No active profile — let legacy system handle it
            Log.Info("Area '{AreaResRef}': no active dynamic profile found — using legacy spawn system.", areaResRef);
            return;
        }

        Log.Info("Area '{AreaResRef}': active dynamic profile '{ProfileName}' found — using new spawn system.",
            areaResRef, profile.Name);

        // Check no_spawn
        if (NWScript.GetLocalInt(area, "no_spawn") == NWScript.TRUE) return;

        // ── OnAreaEnter groups: pre-populate ALL triggers in the area on first entry ──
        bool hasAreaEnterGroups = profile.SpawnGroups
            .Any(g => g.DistributionMethod == DistributionMethod.OnAreaEnter);

        if (hasAreaEnterGroups && NWScript.GetLocalInt(area, AreaEnterSpawnedFlag) != CooldownFlagVar)
        {
            SpawnOnAreaEnterGroups(area, player, profile);
        }

        // ── Trigger-local groups (non-OnAreaEnter) at this specific trigger ──
        if (IsDynamicCooldownActive(obj.Trigger, profile.CooldownSeconds))
        {
            player.SendServerMessage("You see signs of recent fighting here.");
            NWScript.SetLocalInt(obj.Trigger, DynamicHandledFlag, NWScript.TRUE);
            return;
        }

        // Build encounter context
        EncounterContext context = BuildContext(obj.Trigger, area, player, profile);

        // Execute dynamic spawn (non-OnAreaEnter groups only)
        _spawner.SpawnEncounter(obj.Trigger, profile, context);

        // Set cooldown
        InitDynamicCooldown(obj.Trigger, profile.CooldownSeconds);

        // Flag trigger as handled so legacy EncounterService skips
        NWScript.SetLocalInt(obj.Trigger, DynamicHandledFlag, NWScript.TRUE);
    }

    /// <summary>
    /// Spawns <see cref="DistributionMethod.OnAreaEnter"/> groups at every
    /// <c>db_spawntrigger</c> in the area. Called once per area cooldown cycle
    /// on the first trigger entry. Sets an area-level flag and cooldown so
    /// subsequent trigger entries (or other players) don't re-spawn.
    /// </summary>
    private void SpawnOnAreaEnterGroups(NwArea area, NwPlayer player, SpawnProfile profile)
    {
        List<NwTrigger> areaTriggers = NwObject.FindObjectsWithTag<NwTrigger>("db_spawntrigger")
            .Where(t => t.Area == area)
            .ToList();

        if (areaTriggers.Count == 0)
        {
            Log.Debug("Area '{AreaResRef}': no db_spawntrigger triggers found for OnAreaEnter.", area.ResRef);
            return;
        }

        // Mark area as spawned immediately to prevent races with other players
        NWScript.SetLocalInt(area, AreaEnterSpawnedFlag, CooldownFlagVar);

        int triggersSpawned = 0;
        foreach (NwTrigger trigger in areaTriggers)
        {
            EncounterContext context = BuildContext(trigger, area, player, profile);

            bool spawned = _spawner.SpawnAreaEnterEncounter(trigger, profile, context);
            if (!spawned) continue;

            // Set per-trigger cooldown so normal trigger-enter doesn't double-fire
            InitDynamicCooldown(trigger, profile.CooldownSeconds);
            NWScript.SetLocalInt(trigger, DynamicHandledFlag, NWScript.TRUE);
            triggersSpawned++;
        }

        // Schedule area flag reset after cooldown so the area can re-spawn later
        NWScript.DelayCommand(profile.CooldownSeconds,
            () => NWScript.SetLocalInt(area, AreaEnterSpawnedFlag, NWScript.FALSE));

        Log.Info("OnAreaEnter spawn for profile '{Name}' in area '{Area}': {Count}/{Total} triggers spawned.",
            profile.Name, area.ResRef, triggersSpawned, areaTriggers.Count);
    }

    /// <summary>
    /// Boss-only trigger handler. When a player enters a <c>db_bosstrigger</c>, only boss
    /// spawning logic runs (no regular mob spawn groups). The boss pool is evaluated from the
    /// area's <see cref="SpawnProfile"/>. Falls back to legacy if no active profile exists.
    /// </summary>
    private void OnBossTriggerEnter(TriggerEvents.OnEnter obj)
    {
        NWScript.SetLocalInt(obj.Trigger, DynamicHandledFlag, NWScript.FALSE);

        if (!obj.EnteringObject.IsPlayerControlled(out NwPlayer? player)) return;
        if (player.IsDM || player.IsPlayerDM) return;

        NwArea? area = obj.Trigger.Area;
        if (area == null) return;

        string areaResRef = area.ResRef;

        if (!_profileCache.TryGetValue(areaResRef, out SpawnProfile? profile))
        {
            Log.Info("Area '{AreaResRef}': no active dynamic profile for boss trigger — using legacy boss system.", areaResRef);
            return;
        }

        if (profile.BossSpawnChancePercent <= 0 || profile.BossConfigs.Count == 0)
        {
            Log.Debug("Area '{AreaResRef}': profile '{ProfileName}' has no boss pool configured.", areaResRef, profile.Name);
            return;
        }

        // Check no_spawn
        if (NWScript.GetLocalInt(area, "no_spawn") == NWScript.TRUE) return;

        // Check dynamic cooldown (boss triggers share the same cooldown mechanism)
        if (IsDynamicCooldownActive(obj.Trigger, profile.CooldownSeconds))
        {
            player.SendServerMessage("You see signs of recent fighting here.");
            NWScript.SetLocalInt(obj.Trigger, DynamicHandledFlag, NWScript.TRUE);
            return;
        }

        EncounterContext context = BuildContext(obj.Trigger, area, player, profile);

        // Execute boss-only spawn
        _spawner.SpawnBossOnly(obj.Trigger, profile, context);

        InitDynamicCooldown(obj.Trigger, profile.CooldownSeconds);
        NWScript.SetLocalInt(obj.Trigger, DynamicHandledFlag, NWScript.TRUE);
    }

    private EncounterContext BuildContext(NwTrigger trigger, NwArea area, NwPlayer player, SpawnProfile profile)
    {
        // Count party members in the same area
        int partySize = player.PartyMembers
            .Count(pm => pm.LoginCreature?.Area == area);

        // Get game time
        int hour = NWScript.GetTimeHour();
        int minute = NWScript.GetTimeMinute();
        TimeSpan gameTime = new(hour, minute, 0);

        // Check if this area belongs to a region
        bool isInRegion = _regionSubsystem.IsAreaInRegion(area.ResRef);

        // Only pull chaos state from the region subsystem if the area is registered in a region.
        // Unregistered areas get ChaosState.Default (all zeros) — no chaos influence, just base
        // mutations (profile bonuses applied at 1.0× scaling).
        ChaosState chaos = isInRegion
            ? _regionSubsystem.GetChaosForAreaAsync(area.ResRef).GetAwaiter().GetResult()
            : ChaosState.Default;

        // Resolve region tag only for registered areas
        string? regionTag = isInRegion
            ? _regionSubsystem.GetRegionTagForArea(area.ResRef)
            : null;

        // Capture player location for PlayerProximity distribution
        IntPtr playerLocation = player.LoginCreature != null
            ? NWScript.GetLocation(player.LoginCreature)
            : IntPtr.Zero;

        return new EncounterContext
        {
            AreaResRef = area.ResRef,
            PartySize = partySize,
            GameTime = gameTime,
            Chaos = chaos,
            RegionTag = regionTag,
            IsInRegion = isInRegion,
            Trigger = trigger,
            Area = area,
            PlayerLocation = playerLocation
        };
    }

    private static bool IsDynamicCooldownActive(NwTrigger trigger, int cooldownSeconds)
    {
        if (NWScript.GetLocalInt(trigger, CooldownActiveVar) != CooldownFlagVar)
            return false;

        int startTime = NWScript.GetLocalInt(trigger, CooldownStartVar);
        int now = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
        return now - startTime <= cooldownSeconds;
    }

    private static void InitDynamicCooldown(NwTrigger trigger, int cooldownSeconds)
    {
        NWScript.SetLocalInt(trigger, CooldownStartVar, (int)DateTimeOffset.Now.ToUnixTimeSeconds());
        NWScript.SetLocalInt(trigger, CooldownActiveVar, CooldownFlagVar);
        NWScript.DelayCommand(cooldownSeconds,
            () => NWScript.SetLocalInt(trigger, CooldownActiveVar, NWScript.FALSE));
    }
}
