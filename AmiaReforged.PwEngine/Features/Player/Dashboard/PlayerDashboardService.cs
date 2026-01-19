using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;
using System.Numerics;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard;

/// <summary>
/// Handles the player dashboard that opens when a player attempts to rest.
/// This replaces the NWScript rest system (mod_pla_rest, inc_td_rest, ca_rest_rest).
/// </summary>
[ServiceBinding(typeof(PlayerDashboardService))]
public class PlayerDashboardService
{
    private readonly WindowDirector _director;
    private readonly Lazy<BlackguardAuraService> _blackguardAuraService;

    // Static method that can be called from mod_pla_death script
    public static void CleanupRestOnDeath(NwCreature creature)
    {
        NwPlayer? player = creature.ControllingPlayer;
        if (player == null) return;

        // Remove blindness effect
        foreach (Effect effect in creature.ActiveEffects)
        {
            if (effect.Tag == "RestBlindness")
            {
                creature.RemoveEffect(effect);
            }
        }

        // Clean up rest variables
        NWScript.DeleteLocalInt(creature, sVarName: "rest_start");
        NWScript.DeleteLocalInt(creature, sVarName: "AR_RestChoice");

        // Force clear all actions multiple times to try to unstick UI
        creature.ClearActionQueue();
        NWScript.AssignCommand(creature, () => NWScript.ClearAllActions());

        // Delay and try again
        NwTask.Run(async () =>
        {
            await NwTask.Delay(TimeSpan.FromMilliseconds(100));
            creature.ClearActionQueue();
            NWScript.AssignCommand(creature, () => NWScript.ClearAllActions());
        });
    }

    // Rest system constants (from inc_td_rest)
    private const int REST_BLOCK_MINUTES = 15;
    private const float HOSTILE_RANGE = 20.0f;
    private const int AMBUSH_RATE_PERCENT = 20;
    private const int AMBUSH_RATE_PERCENT_CAP = 1;
    private const int MAX_PLCS_TAKEN_INTO_ACCOUNT = 4;
    private const float CAMP_GEAR_MAX_RANGE = 10.0f;
    private const int AMBUSH_SPAWN_AMOUNT = 6;
    private const int AMBUSH_SPAWN_MIN = 2;
    private const int MINUTES_BETWEEN_POSSIBLE_AMBUSH = REST_BLOCK_MINUTES;
    private const string SPAWNPOINT_TAG = "ds_spwn";

    public PlayerDashboardService(
        WindowDirector director,
        Lazy<BlackguardAuraService> blackguardAuraService)
    {
        _director = director;
        _blackguardAuraService = blackguardAuraService;
        NwModule.Instance.OnPlayerRest += OnPlayerRest;
    }

    private void OnPlayerRest(ModuleEvents.OnPlayerRest obj)
    {
        NwPlayer player = obj.Player;
        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        switch (obj.RestEventType)
        {
            case RestEventType.Started:
                OnRestStarted(player, creature);
                break;
            case RestEventType.Finished:
                OnRestFinished(player, creature);
                break;
            case RestEventType.Cancelled:
                OnRestCancelled(player, creature);
                break;
        }
    }

    private void OnRestStarted(NwPlayer player, NwCreature creature)
    {

        NwArea? area = creature.Area;
        if (area == null) return;

        // Subscribe to death event to handle death during rest
        creature.OnDeath += HandleDeathDuringRest;

        // Set action script for emotes (from mod_pla_rest)
        NWScript.SetLocalString(creature, sVarName: "ds_action", "ds_emotes");

        // A hack just to ensure DM healing doesn't screw over PCs
        if (!creature.IsDead)
        {
            NwItem? pcKey = creature.GetItemInSlot(InventorySlot.CreatureSkin) ??
                           creature.Inventory.Items.FirstOrDefault(i => i.ResRef == "ds_pckey");
            if (pcKey != null)
            {
                NWScript.DeleteLocalString(pcKey, sVarName: "dead_in");
            }
        }

        // DM check - open DM tools instead
        if (player.IsDM)
        {
            creature.ClearActionQueue();
            // DM tools would open here, but that's handled by a separate service
            return;
        }

        // Horse check - Make sure they are dismounted before allowing them to rest
        if (NWScript.GetLocalInt(creature, sVarName: "mounted") == 1)
        {
            player.SendServerMessage("You must dismount before resting!", ColorConstants.Orange);
            creature.ClearActionQueue();
            return;
        }

        // Check if the area is marked "NO_REST" and abort if so
        if (NWScript.GetLocalInt(area, sVarName: "NO_REST") == NWScript.TRUE)
        {
            // unless PC standing in safe rest trigger, with nearest door closed
            uint nearestDoorId = NWScript.GetNearestObject(NWScript.OBJECT_TYPE_DOOR, creature);
            NwDoor? nearestDoor = nearestDoorId.ToNwObject<NwDoor>();
            bool isInSafeRestTrigger = IsInsideTrigger(creature, "X0_SAFEREST");

            if (isInSafeRestTrigger && nearestDoor != null && !nearestDoor.IsOpen)
            {
                // continue - they're in a safe spot
            }
            else
            {
                player.SendServerMessage("It is far too dangerous to rest in this area. You'll have to find a secure place to rest.", ColorConstants.Orange);
                creature.ClearActionQueue();
                return;
            }
        }

        // Player resting invokes a dashboard; check if they've made a choice
        int restChoice = NWScript.GetLocalInt(creature, sVarName: "AR_RestChoice");

        if (restChoice == 0)
        {
            // Check if dashboard is already open - if so, close it (toggle behavior)
            if (_director.IsWindowOpen(player, typeof(PlayerDashboardPresenter)))
            {
                creature.ClearActionQueue();
                _director.CloseWindow(player, typeof(PlayerDashboardPresenter));
                return;
            }

            // Open the dashboard instead of allowing rest
            creature.ClearActionQueue();
            _ = OpenDashboard(player);
            return;
        }


        // If we get here, AR_RestChoice = 1, meaning the player clicked Rest in the dashboard
        // The Presenter already checked the cooldown, so we can proceed with other checks

        // Check for hostile creatures
        NwCreature? nearestEnemy = creature.GetNearestCreatures(CreatureTypeFilter.Reputation(ReputationType.Enemy)).FirstOrDefault();
        if (nearestEnemy != null)
        {
            float distance = creature.Distance(nearestEnemy);
            if (distance > 0.0f && distance <= HOSTILE_RANGE)
            {
                player.SendServerMessage("You cannot rest while enemies are near.", ColorConstants.Orange);
                creature.ClearActionQueue();
                return;
            }
        }

        // Apply blindness effect if configured
        ApplyRestBlindness(creature, true);

        // Handle rest ambush system (happens even in FreeRest areas)
        HandleRestAmbush(player, creature, area);

        // Check if a free resting area (this only affects cooldown, not ambushes)
        int isFreeRest = NWScript.GetLocalInt(area, sVarName: "FreeRest");

        if (isFreeRest == NWScript.TRUE)
        {
            // Allow rest to proceed without cooldown
            return;
        }

        // Save rest start time
        NWScript.SetLocalInt(creature, sVarName: "rest_start", GetCurrentSecond());
    }

    private void OnRestFinished(NwPlayer player, NwCreature creature)
    {
        // Unsubscribe from death event - rest is complete
        creature.OnDeath -= HandleDeathDuringRest;

        // Check if creature died during rest (e.g., from CON buff wearing off)
        // HP <= 0 is the most reliable check for death
        if (creature.HP <= 0)
        {
            // Remove blindness effect if it was applied
            ApplyRestBlindness(creature, false);

            // Clean up variables
            NWScript.DeleteLocalInt(creature, sVarName: "rest_start");
            NWScript.DeleteLocalInt(creature, sVarName: "AR_RestChoice");

            // Clear action queue to try to unstick the rest UI
            creature.ClearActionQueue();

            return;
        }

        // Remove blindness effect
        ApplyRestBlindness(creature, false);

        // Apply Dragon Disciple bonuses (from mod_pla_rest)
        ApplyDragonDiscipleBonuses(creature);

        // Apply Blackguard Aura of Despair (from mod_pla_rest)
        ApplyBlackguardAura(creature);

        // Re-initialize Racial Traits (this would call your ApplyAreaAndRaceEffects)
        // ApplyAreaAndRaceEffects(creature, 1);

        // Export player (persist)
        NWScript.ExportSingleCharacter(creature);

        // Delete various temporary local ints
        NWScript.DeleteLocalInt(creature, sVarName: "cs_vampirefang1");
        NWScript.DeleteLocalInt(creature, sVarName: "cs_kravenbook1");
        NWScript.DeleteLocalInt(creature, sVarName: "TwistOfFate");
        NWScript.DeleteLocalInt(creature, sVarName: "cus_feat_use_act");
        NWScript.DeleteLocalInt(creature, sVarName: "cus_feat_use_ins");

        // Remove monk activatable from pckey
        NwItem? pcKey = creature.Inventory.Items.FirstOrDefault(i => i.ResRef == "ds_pckey");
        if (pcKey != null)
        {
            NWScript.DeleteLocalInt(pcKey, sVarName: "monkprc");
        }

        // Set rest block timer
        SetBlockTime(creature, REST_BLOCK_MINUTES, "AR_LastRestHour");

        string timeString = REST_BLOCK_MINUTES > 1 ? $"{REST_BLOCK_MINUTES} minutes" : "1 minute";
        player.SendServerMessage($"You rested 100%. You have to wait {timeString} before you can rest again.", ColorConstants.Green);

        NWScript.DeleteLocalInt(creature, sVarName: "rest_start");
        NWScript.DeleteLocalInt(creature, sVarName: "AR_RestChoice");
    }

    private void OnRestCancelled(NwPlayer player, NwCreature creature)
    {
        // Unsubscribe from death event - rest is cancelled
        creature.OnDeath -= HandleDeathDuringRest;

        // Check if creature died during rest (e.g., from CON buff wearing off)
        if (creature.HP <= 0)
        {
            // Remove blindness effect if it was applied
            ApplyRestBlindness(creature, false);

            // Clean up variables
            NWScript.DeleteLocalInt(creature, sVarName: "rest_start");
            NWScript.DeleteLocalInt(creature, sVarName: "AR_RestChoice");

            // Clear action queue
            creature.ClearActionQueue();

            return;
        }

        // Remove blindness effect
        ApplyRestBlindness(creature, false);

        // Check if cancelled due to hostiles
        NwCreature? nearestEnemy = creature.GetNearestCreatures(CreatureTypeFilter.Reputation(ReputationType.Enemy)).FirstOrDefault();
        float distance = nearestEnemy != null ? creature.Distance(nearestEnemy) : 0.0f;

        int restStartTime = NWScript.GetLocalInt(creature, sVarName: "rest_start");

        // Assume the rest was canceled by a hostile; ambush. No blocking partial rests then
        if ((distance > 0.0f && distance <= HOSTILE_RANGE) || restStartTime <= 0)
        {
            NWScript.DeleteLocalInt(creature, sVarName: "rest_start");
            NWScript.DeleteLocalInt(creature, sVarName: "AR_RestChoice");
            return;
        }

        // Calculate partial rest percentage
        int hitDice = creature.Level;
        int normalRestTime = (hitDice / 2) + 10;
        int currentTime = GetCurrentSecond();
        int timeRested = currentTime - restStartTime;

        float restedPercentage = (float)timeRested / normalRestTime;
        int blockMinutes = (int)(REST_BLOCK_MINUTES * restedPercentage);

        int restedPercent = (int)(restedPercentage * 100);
        string timeString = blockMinutes > 1 ? $"{blockMinutes} minutes" : $"{blockMinutes} minute";

        player.SendServerMessage($"You rested {restedPercent}%. You have to wait {timeString} before you can rest again.", ColorConstants.Yellow);

        // Clean up rest variables
        NWScript.DeleteLocalInt(creature, sVarName: "rest_start");
        NWScript.DeleteLocalInt(creature, sVarName: "AR_RestChoice");

        SetBlockTime(creature, blockMinutes, "AR_LastRestHour");
    }

    private void HandleDeathDuringRest(CreatureEvents.OnDeath deathEvent)
    {
        NwCreature creature = deathEvent.KilledCreature;
        NwPlayer? player = creature.ControllingPlayer;

        if (player == null) return;

        // Player died during rest - unsubscribe immediately to prevent multiple calls
        creature.OnDeath -= HandleDeathDuringRest;

        // Use the static cleanup method
        CleanupRestOnDeath(creature);

        // WORKAROUND: Immediately resurrect them to clear the stuck rest UI,
        // then kill them again so the death is still valid
        _ = NwTask.Run(async () =>
        {
            // Wait a moment for death to process
            await NwTask.Delay(TimeSpan.FromMilliseconds(100));

            // Resurrect to clear stuck UI
            NWScript.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT,
                NWScript.EffectResurrection(), creature);

            // Wait another moment
            await NwTask.Delay(TimeSpan.FromMilliseconds(100));

            // Kill them again so the death is valid (they should respawn/wait for raise normally)
            Effect death = Effect.Death();
            creature.ApplyEffect(EffectDuration.Instant, death);
        });
    }

    public Task OpenDashboard(NwPlayer player)
    {
        // Check if dashboard is already open for this player
        if (_director.IsWindowOpen(player, typeof(PlayerDashboardPresenter)))
        {
            return Task.CompletedTask;
        }

        IScryPresenter presenter = PlayerDashboardFactory.OpenDashboard(player);

        InjectionService? injector = AnvilCore.GetService<InjectionService>();
        if (injector is null)
        {
            player.FloatingTextString(
                message: "Failed to load the dashboard due to missing DI container. Screenshot this and report it as a bug.",
                false);
            return Task.CompletedTask;
        }

        injector.Inject(presenter);
        _director.OpenWindow(presenter);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Checks if the player can rest based on cooldown timer.
    /// Returns the number of seconds remaining on cooldown, or 0 if they can rest.
    /// </summary>
    public int GetRestCooldownRemaining(NwCreature creature)
    {
        // Check if in a free rest area
        if (creature.Area != null && NWScript.GetLocalInt(creature.Area, sVarName: "FreeRest") == NWScript.TRUE)
        {
            return 0;
        }

        return GetBlockTimeRemaining(creature, "AR_LastRestHour");
    }

    #region Helper Methods

    private void ApplyDragonDiscipleBonuses(NwCreature creature)
    {
        // Remove any lingering DD effects
        foreach (Effect effect in creature.ActiveEffects)
        {
            if (NWScript.GetEffectTag(effect) == "DDBonuses")
            {
                creature.RemoveEffect(effect);
            }
        }

        // RDD SR and Immunity Calculations
        int ddLevels = creature.GetClassInfo(ClassType.DragonDisciple)?.Level ?? 0;
        if (ddLevels < 10) return;

        int srIncrease = 0;
        int immunityIncrease = 0;

        // SR
        if (ddLevels == 20)
            srIncrease = 32;
        else if (ddLevels >= 18)
            srIncrease = 24;

        // Immunity % levels
        if (ddLevels == 20)
            immunityIncrease = 100;
        else if (ddLevels >= 15)
            immunityIncrease = 75;
        else
            immunityIncrease = 50; // >= 10

        // Immunity Type - check for feats
        DamageType immunityType = DamageType.Fire; // default

        if (NWScript.GetHasFeat(965, creature) == NWScript.TRUE || NWScript.GetHasFeat(1210, creature) == NWScript.TRUE)
            immunityType = DamageType.Fire;
        else if (NWScript.GetHasFeat(1211, creature) == NWScript.TRUE)
            immunityType = DamageType.Cold;
        else if (NWScript.GetHasFeat(1212, creature) == NWScript.TRUE)
            immunityType = DamageType.Negative;
        else if (NWScript.GetHasFeat(1213, creature) == NWScript.TRUE || NWScript.GetHasFeat(1214, creature) == NWScript.TRUE)
            immunityType = DamageType.Acid;
        else if (NWScript.GetHasFeat(1215, creature) == NWScript.TRUE)
            immunityType = DamageType.Electrical;

        Effect immunityEffect = Effect.DamageImmunityIncrease(immunityType, immunityIncrease);
        Effect srEffect = Effect.SpellResistanceIncrease(srIncrease);
        Effect linked = Effect.LinkEffects(immunityEffect, srEffect);
        linked.Tag = "DDBonuses";

        creature.ApplyEffect(EffectDuration.Permanent, linked);
    }

    private void ApplyBlackguardAura(NwCreature creature)
    {
        int bgLevels = creature.GetClassInfo((ClassType)31)?.Level ?? 0; // Blackguard

        if (bgLevels >= 3)
        {
            // Aura of Despair: -2 penalty to all saves for enemies within 10 feet
            // Ported from bg_despair.nss

            // First, remove any existing aura by checking effect tags
            foreach (Effect effect in creature.ActiveEffects)
            {
                string tag = effect.Tag ?? "";
                if (tag == "BlackguardAuraOfDespair")
                {
                    creature.RemoveEffect(effect);
                }
            }

            // Create AOE effect using BlackguardAuraService
            // This creates the effect with proper ScriptCallbackHandles for OnEnter/OnExit
            Effect aura = _blackguardAuraService.Value.CreateAuraOfDespair(creature);
            creature.ApplyEffect(EffectDuration.Permanent, aura);
        }
    }


    private void ApplyRestBlindness(NwCreature creature, bool apply)
    {
        if (apply)
        {
            Effect blindness = Effect.Blindness();
            blindness.SubType = EffectSubType.Supernatural;
            creature.ApplyEffect(EffectDuration.Temporary, blindness, TimeSpan.FromSeconds(60));
        }
        else
        {
            foreach (Effect effect in creature.ActiveEffects)
            {
                if (effect.EffectType == EffectType.Blindness &&
                    NWScript.GetEffectSubType(effect) == NWScript.SUBTYPE_SUPERNATURAL)
                {
                    creature.RemoveEffect(effect);
                    break;
                }
            }
        }
    }

    private void HandleRestAmbush(NwPlayer player, NwCreature creature, NwArea area)
    {
        // Check if already had an ambush recently
        if (GetBlockTimeRemaining(creature, "Ambush_block") > 0)
        {
            player.SendServerMessage("Possibility : 0%");
            return;
        }

        // If resting on a safe rest trigger, don't ambush
        if (IsInsideTrigger(creature, "X0_SAFEREST"))
        {
            return;
        }

        // Check if area has critters set up
        string daySpawn1 = NWScript.GetLocalString(area, sVarName: "day_spawn1");
        string nightSpawn1 = NWScript.GetLocalString(area, sVarName: "night_spawn1");

        bool hasCritters = daySpawn1 != "" || nightSpawn1 != "";

        if (!hasCritters)
        {
            player.SendServerMessage("Possibility : 0%");
            return;
        }

        // Check proximity to spawn point
        var allWaypoints = area.FindObjectsOfTypeInArea<NwWaypoint>().ToList();

        NwWaypoint? spawnPoint = area.FindObjectsOfTypeInArea<NwWaypoint>()
            .FirstOrDefault(wp => wp.Tag == SPAWNPOINT_TAG && creature.Distance(wp) <= 15.0f);

        if (spawnPoint == null)
        {
            player.SendServerMessage("Possibility : 0%");
            return;
        }

        float distanceToSpawn = creature.Distance(spawnPoint);

        // Calculate ambush possibility
        int campingGearCount = GetCampPLCsWithinRange(creature);

        // Calculate distance from area center
        int areaWidth = NWScript.GetAreaSize(NWScript.AREA_WIDTH, area);
        int areaHeight = NWScript.GetAreaSize(NWScript.AREA_HEIGHT, area);

        float centerX = (areaWidth * 10f) / 2f;
        float centerY = (areaHeight * 10f) / 2f;

        float distanceFromCenter = MathF.Sqrt(
            MathF.Pow(creature.Position.X - centerX, 2) +
            MathF.Pow(creature.Position.Y - centerY, 2)
        );
        int tilesBetween = (int)(distanceFromCenter / 10f);

        int ambushRate = NWScript.GetLocalInt(area, sVarName: "ambush");
        int baseRate = ambushRate > 0 ? ambushRate : AMBUSH_RATE_PERCENT;

        int possibility = baseRate - campingGearCount - tilesBetween;

        if (possibility <= 0)
            possibility = AMBUSH_RATE_PERCENT_CAP;

        // Boost if close to spawn
        if (distanceToSpawn > 0)
        {
            int proximityBonus = (int)(15 - distanceToSpawn);
            possibility += proximityBonus;
        }

        int roll = Random.Shared.Next(1, 101); // d100

        player.SendServerMessage($"Possibility : {possibility}%");
        player.SendServerMessage($"Ambush Roll : {roll}");

        // Test against d100 - if successful, spawn ambush IMMEDIATELY to interrupt rest
        if (roll <= possibility)
        {
            player.SendServerMessage("[AMBUSH DEBUG] Roll success! Spawning ambush NOW...", ColorConstants.Green);
            DoAmbush(player, creature, area, spawnPoint);
        }
        else
        {
            player.SendServerMessage("[AMBUSH DEBUG] Roll failed - no ambush", ColorConstants.Yellow);
        }
    }

    private void DoAmbush(NwPlayer player, NwCreature creature, NwArea area, NwWaypoint spawnPoint)
    {
        bool spawned = SpawnAmbush(creature, area, spawnPoint);

        if (spawned)
        {
            NWScript.FloatingTextStringOnCreature("*Ambush!*", creature, NWScript.FALSE);
            SetBlockTime(creature, MINUTES_BETWEEN_POSSIBLE_AMBUSH, "Ambush_block");
            creature.ClearActionQueue();
        }
        else
        {
            player.SendServerMessage("ERROR: spawning failed!");
        }
    }

    private bool SpawnAmbush(NwCreature creature, NwArea area, NwWaypoint spawnPoint)
    {
        // Check if it's night and if variable spawns are enabled
        bool isNight = NWScript.GetIsNight() == NWScript.TRUE;
        bool spawnsVary = NWScript.GetLocalInt(area, sVarName: "spawns_vary") == 1;

        string spawnVariable = "day_spawn";
        if (isNight && spawnsVary)
        {
            string nightCheck = NWScript.GetLocalString(area, sVarName: "night_spawn1");
            if (!string.IsNullOrEmpty(nightCheck))
                spawnVariable = "night_spawn";
        }

        // Count how many spawn variables are set
        int spawnCount = 0;
        while (NWScript.GetLocalString(area, sVarName: $"{spawnVariable}{spawnCount + 1}") != "")
        {
            spawnCount++;
        }

        if (spawnCount == 0)
        {
            creature.ControllingPlayer?.SendServerMessage("Spawns not set.");
            return false;
        }

        // Spawn creatures
        int numToSpawn = Random.Shared.Next(1, AMBUSH_SPAWN_AMOUNT + 1);
        if (numToSpawn < AMBUSH_SPAWN_MIN)
            numToSpawn = AMBUSH_SPAWN_MIN;

        for (int i = 0; i < numToSpawn; i++)
        {
            float angle = Random.Shared.Next(0, 361);
            Vector3 offset = new Vector3(
                5.0f * MathF.Cos(angle * MathF.PI / 180f),
                5.0f * MathF.Sin(angle * MathF.PI / 180f),
                0f
            );

            Location spawnLoc = Location.Create(area, spawnPoint.Position + offset, angle);

            int randomIndex = Random.Shared.Next(1, spawnCount + 1);
            string resRef = NWScript.GetLocalString(area, sVarName: $"{spawnVariable}{randomIndex}");

            if (!string.IsNullOrEmpty(resRef))
            {
                NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, resRef, spawnLoc);
            }
        }

        return true;
    }

    private int GetCampPLCsWithinRange(NwCreature creature)
    {
        int count = 0;
        int loop = 1;

        while (count < MAX_PLCS_TAKEN_INTO_ACCOUNT)
        {
            NwGameObject? plc = NWScript.GetNearestObject(NWScript.OBJECT_TYPE_PLACEABLE, creature, loop).ToNwObject<NwPlaceable>();

            if (plc == null || !plc.IsValid)
                break;

            float distance = creature.Distance(plc);
            if (distance > CAMP_GEAR_MAX_RANGE)
                break;

            if (NWScript.GetLocalInt(plc, sVarName: "ds_cg") == 1)
            {
                count++;
            }

            loop++;
        }

        return count;
    }

    private bool IsInsideTrigger(NwCreature creature, string triggerTag)
    {
        if (creature.Area == null) return false;

        foreach (NwTrigger trigger in creature.Area.FindObjectsOfTypeInArea<NwTrigger>())
        {
            if (trigger.Tag == triggerTag)
            {
                // Use NWScript to check if creature is in the trigger
                if (NWScript.GetIsInSubArea(creature, trigger) == NWScript.TRUE)
                    return true;
            }
        }
        return false;
    }

    private int GetBlockTimeRemaining(NwCreature creature, string varName)
    {
        int blockTime = NWScript.GetLocalInt(creature, sVarName: varName);
        if (blockTime == 0)
            return 0;

        int currentTime = GetCurrentSecond();
        int timeRemaining = blockTime - currentTime;

        return timeRemaining > 0 ? timeRemaining : 0;
    }

    private void SetBlockTime(NwCreature creature, int minutes, string varName)
    {
        int currentTime = GetCurrentSecond();
        int blockUntil = currentTime + (minutes * 60);
        NWScript.SetLocalInt(creature, sVarName: varName, blockUntil);
    }

    private int GetCurrentSecond()
    {
        // Get Unix timestamp in seconds
        return (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    #endregion
}
