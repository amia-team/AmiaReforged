using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Spells;

/// <summary>
/// Grants divine casters (Cleric, Druid, Ranger, Paladin) access to all spells for circles
/// they've unlocked through prestige class CL stacking.
///
/// For example, a Cleric 9/Blackguard 16 has effective CL 20, which grants access to 9th circle spells
/// even though their actual Cleric level is only 9 (which normally grants 5th circle max).
///
/// Also handles domain spells for Clerics.
/// </summary>
[ServiceBinding(typeof(DivineCasterSpellAccessService))]
public class DivineCasterSpellAccessService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly DivineSpellCache _spellCache;

    // Prefix for storing granted spells on ds_pckey
    private const string PrestigeSpellPrefix = "DIVINE_PRESTIGE_SPELL_";

    // Divine caster classes this service handles
    private static readonly ClassType[] DivineCasterClasses =
    {
        ClassType.Cleric,
        ClassType.Druid,
        ClassType.Paladin,
        ClassType.Ranger
    };

    public DivineCasterSpellAccessService(DivineSpellCache spellCache, EventService eventService)
    {
        try
        {
            Log.Info("=== INITIALIZING DivineCasterSpellAccessService ===");
            Log.Info("Service instantiation detected - this appears in logs IMMEDIATELY");
            _spellCache = spellCache;

            // Subscribe to events
            Log.Info("Subscribing to OnClientEnter event...");
            NwModule.Instance.OnClientEnter += OnClientEnter;
            Log.Info("✓ OnClientEnter subscribed");

            Log.Info("Subscribing to OnLevelUp event...");
            eventService.SubscribeAll<OnLevelUp, OnLevelUp.Factory>(OnLevelUp, EventCallbackType.After);
            Log.Info("✓ OnLevelUp subscribed");

            Log.Info("Subscribing to OnLevelDown event...");
            eventService.SubscribeAll<OnLevelDown, OnLevelDown.Factory>(OnLevelDown, EventCallbackType.After);
            Log.Info("✓ OnLevelDown subscribed");

            Log.Info("=== DivineCasterSpellAccessService initialized successfully ===");
        }
        catch (Exception ex)
        {
            Log.Error($"FAILED to initialize DivineCasterSpellAccessService: {ex.Message}");
            Log.Error($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    private void OnClientEnter(ModuleEvents.OnClientEnter obj)
    {
        try
        {
            Log.Info($"=== OnClientEnter fired for player (IsDM={obj.Player.IsDM}) ===");

            if (obj.Player.IsDM)
            {
                Log.Info($"Player is DM, skipping spell access processing");
                return;
            }

            if (obj.Player.LoginCreature == null)
            {
                Log.Warn($"LoginCreature is null for player");
                return;
            }

            Log.Info($"Scheduling spell access processing with 3s delay for {obj.Player.LoginCreature.Name}...");

            // Longer delay to ensure character is fully loaded AND PrestigeSpellSlotService
            // has equipped the creature hide (which creates spell slot structures).
            NwTask.Run(async () =>
            {
                try
                {
                    Log.Info($"Async task started, waiting 3 seconds...");
                    await NwTask.Delay(TimeSpan.FromSeconds(3));
                    Log.Info($"3 second delay complete, switching to main thread...");
                    await NwTask.SwitchToMainThread();
                    Log.Info($"Main thread acquired, checking if creature is valid...");

                    if (obj.Player.LoginCreature?.IsValid == true)
                    {
                        Log.Info($"Creature is valid, calling ProcessCreatureSpellAccess...");
                        ProcessCreatureSpellAccess(obj.Player.LoginCreature, isLogin: true);
                    }
                    else
                    {
                        Log.Warn($"Creature is no longer valid or is null after delay");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Exception in OnClientEnter async task: {ex.Message}");
                    Log.Error($"Stack trace: {ex.StackTrace}");
                }
            });
        }
        catch (Exception ex)
        {
            Log.Error($"Exception in OnClientEnter: {ex.Message}");
            Log.Error($"Stack trace: {ex.StackTrace}");
        }
    }

    private void OnLevelUp(OnLevelUp obj)
    {
        try
        {
            Log.Info($"=== OnLevelUp fired for {obj.Creature.Name} ===");

            if (!obj.Creature.IsPlayerControlled(out _))
            {
                Log.Info($"Creature is not player controlled, skipping");
                return;
            }

            Log.Info($"Scheduling spell access processing with 3s delay for {obj.Creature.Name} (level up)...");

            NwCreature creature = obj.Creature;
            NwTask.Run(async () =>
            {
                try
                {
                    Log.Info($"LevelUp async task started, waiting 3 seconds...");
                    await NwTask.Delay(TimeSpan.FromSeconds(3));
                    Log.Info($"3 second delay complete, switching to main thread...");
                    await NwTask.SwitchToMainThread();
                    Log.Info($"Main thread acquired, checking if creature is valid...");

                    if (creature.IsValid)
                    {
                        Log.Info($"Creature is valid, calling ProcessCreatureSpellAccess...");
                        ProcessCreatureSpellAccess(creature, isLogin: false);
                    }
                    else
                    {
                        Log.Warn($"Creature is no longer valid after delay");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Exception in OnLevelUp async task: {ex.Message}");
                    Log.Error($"Stack trace: {ex.StackTrace}");
                }
            });
        }
        catch (Exception ex)
        {
            Log.Error($"Exception in OnLevelUp: {ex.Message}");
            Log.Error($"Stack trace: {ex.StackTrace}");
        }
    }

    private void OnLevelDown(OnLevelDown obj)
    {
        try
        {
            Log.Info($"=== OnLevelDown fired for {obj.Creature.Name} ===");

            if (!obj.Creature.IsPlayerControlled(out _))
            {
                Log.Info($"Creature is not player controlled, skipping");
                return;
            }

            Log.Info($"Scheduling spell access processing with 3s delay for {obj.Creature.Name} (level down)...");

            NwCreature creature = obj.Creature;
            NwTask.Run(async () =>
            {
                try
                {
                    Log.Info($"LevelDown async task started, waiting 3 seconds...");
                    await NwTask.Delay(TimeSpan.FromSeconds(3));
                    Log.Info($"3 second delay complete, switching to main thread...");
                    await NwTask.SwitchToMainThread();
                    Log.Info($"Main thread acquired, checking if creature is valid...");

                    if (creature.IsValid)
                    {
                        Log.Info($"Creature is valid, calling ProcessCreatureSpellAccess...");
                        ProcessCreatureSpellAccess(creature, isLogin: false);
                    }
                    else
                    {
                        Log.Warn($"Creature is no longer valid after delay");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Exception in OnLevelDown async task: {ex.Message}");
                    Log.Error($"Stack trace: {ex.StackTrace}");
                }
            });
        }
        catch (Exception ex)
        {
            Log.Error($"Exception in OnLevelDown: {ex.Message}");
            Log.Error($"Stack trace: {ex.StackTrace}");
        }
    }

    private void ProcessCreatureSpellAccess(NwCreature creature, bool isLogin)
    {
        try
        {
            Log.Info($"=== ProcessCreatureSpellAccess START for {creature.Name} (isLogin={isLogin}) ===");
            bool isPlayer = creature.IsPlayerControlled(out NwPlayer? player);

            Log.Info($"Is player controlled: {isPlayer}");

            if (isPlayer)
            {
                player?.SendServerMessage($"[DEBUG] Starting divine spell access processing (isLogin={isLogin})", ColorConstants.Yellow);
            }

            // Get effective caster levels for all classes
            Log.Info($"Calling EffectiveCasterLevelCalculator...");
            Dictionary<ClassType, int> effectiveLevels = EffectiveCasterLevelCalculator.CalculateAllEffectiveCasterLevels(creature);
            Log.Info($"Effective caster levels calculated: {effectiveLevels.Count} classes");

            if (isPlayer)
            {
                player?.SendServerMessage($"[DEBUG] Calculated {effectiveLevels.Count} effective caster level(s)", ColorConstants.Yellow);
                foreach (var kvp in effectiveLevels)
                {
                    player?.SendServerMessage($"[DEBUG]   {kvp.Key}: CL {kvp.Value}", ColorConstants.Yellow);
                }
            }

            int totalSpellsGranted = 0;

            // Process each divine caster class the creature has
            foreach (ClassType classType in DivineCasterClasses)
            {
                CreatureClassInfo? classInfo = creature.GetClassInfo(classType);
                if (classInfo == null || classInfo.Level == 0)
                {
                    Log.Debug($"  {classType}: Not found or level 0, skipping");
                    continue;
                }

                int actualLevel = classInfo.Level;
                int effectiveLevel = effectiveLevels.TryGetValue(classType, out int el) ? el : actualLevel;

                Log.Info($"  Found {classType}: Actual={actualLevel}, Effective={effectiveLevel}, ClassId={classInfo.Class.Id}, ClassName={classInfo.Class.Name}");

                if (isPlayer)
                {
                    player?.SendServerMessage($"[DEBUG] {classType}: Actual {actualLevel}, Effective {effectiveLevel} (ID={classInfo.Class.Id})", ColorConstants.Yellow);
                }

                // Only process if effective level is higher than actual level
                if (effectiveLevel <= actualLevel)
                {
                    Log.Debug($"  {classType}: Effective CL ({effectiveLevel}) <= Actual Level ({actualLevel}), no bonus spells needed");
                    if (isPlayer)
                    {
                        player?.SendServerMessage($"[DEBUG]   -> No boost needed (effective <= actual)", ColorConstants.Yellow);
                    }
                    continue;
                }

                Log.Info($"  {classType}: Actual Level {actualLevel}, Effective CL {effectiveLevel}");

                // Calculate spell circle access
                int actualMaxCircle = DivineSpellProgressionData.GetMaxSpellCircleForCasterLevel(classType, actualLevel);
                int effectiveMaxCircle = DivineSpellProgressionData.GetMaxSpellCircleForCasterLevel(classType, effectiveLevel);

                if (isPlayer)
                {
                    player?.SendServerMessage($"[DEBUG]   Actual max circle: {actualMaxCircle}, Effective max circle: {effectiveMaxCircle}", ColorConstants.Yellow);
                }

                if (effectiveMaxCircle <= actualMaxCircle)
                {
                    Log.Debug($"    Max circle unchanged ({actualMaxCircle}), no new spells to grant");
                    if (isPlayer)
                    {
                        player?.SendServerMessage($"[DEBUG]   -> Max circle unchanged, skipping", ColorConstants.Yellow);
                    }
                    continue;
                }

                // For partial casters (Ranger/Paladin) under level 4, we need to grant spells starting from circle 1
                // because the engine hasn't created spell structures for them yet.
                // For other casters, we only grant newly accessible circles.
                int startCircle = (classType is ClassType.Ranger or ClassType.Paladin && actualMaxCircle == 0)
                    ? 1
                    : actualMaxCircle + 1;

                Log.Info($"    Granting access to circles {startCircle}-{effectiveMaxCircle} (was {actualMaxCircle}, now {effectiveMaxCircle})");
                if (isPlayer)
                {
                    player?.SendServerMessage($"[DEBUG]   -> Granting circles {startCircle}-{effectiveMaxCircle}", ColorConstants.Cyan);
                }

                // Grant class spells for newly accessible circles
                int granted = GrantClassSpells(creature, classType, classInfo.Class.Id, startCircle, effectiveMaxCircle);
                totalSpellsGranted += granted;

                if (isPlayer)
                {
                    player?.SendServerMessage($"[DEBUG]   -> Granted {granted} class spells", ColorConstants.Cyan);
                }

                // For Clerics, also grant domain spells
                if (classType == ClassType.Cleric)
                {
                    int domainGranted = GrantDomainSpells(creature, classInfo.Class.Id, startCircle, effectiveMaxCircle);
                    totalSpellsGranted += domainGranted;
                    if (isPlayer)
                    {
                        player?.SendServerMessage($"[DEBUG]   -> Granted {domainGranted} domain spells", ColorConstants.Cyan);
                    }
                }
            }

            if (isPlayer)
            {
                player?.SendServerMessage($"[DEBUG] Processing complete. Total spells granted: {totalSpellsGranted}", ColorConstants.Yellow);
            }

            // Send feedback to player
            if (totalSpellsGranted > 0 && isPlayer)
            {
                player?.SendServerMessage(
                    $"Prestige divine casting: {totalSpellsGranted} spell(s) added to your spellbook.",
                    ColorConstants.Cyan);
            }
            else if (totalSpellsGranted == 0 && isPlayer)
            {
                player?.SendServerMessage($"[DEBUG] No spells granted during this processing.", ColorConstants.Orange);
            }

            Log.Info($"=== ProcessCreatureSpellAccess END for {creature.Name} - Total spells granted: {totalSpellsGranted} ===");
        }
        catch (Exception ex)
        {
            Log.Error($"Exception in ProcessCreatureSpellAccess: {ex.Message}");
            Log.Error($"Stack trace: {ex.StackTrace}");

            if (creature.IsPlayerControlled(out NwPlayer? player))
            {
                player.SendServerMessage($"[ERROR] Divine spell processing failed: {ex.Message}", ColorConstants.Red);
            }
        }
    }

    /// <summary>
    /// For partial divine casters (Ranger/Paladin), the NWN engine only creates internal spell
    /// data structures for circles the character naturally has access to based on actual class level.
    /// Full casters (Cleric/Druid) get structures for all 9 circles even at level 1.
    /// Without the structures, AddKnownSpell silently fails. This method forces the engine to
    /// create them by setting remaining spell slots at each level we need, which triggers the
    /// engine to initialize the underlying data structures.
    /// </summary>
    private void EnsureSpellStructuresExist(NwCreature creature, ClassType classType, int classId, int fromCircle, int toCircle)
    {
        if (classType is not (ClassType.Ranger or ClassType.Paladin))
            return;

        bool isPlayer = creature.IsPlayerControlled(out NwPlayer? player);

        // Get effective caster level for this class
        int effectiveLevel = EffectiveCasterLevelCalculator.GetEffectiveCasterLevelForClass(creature, classType);

        if (isPlayer)
        {
            player?.SendServerMessage($"[DEBUG] EnsureSpellStructuresExist: {classType} circles {fromCircle}-{toCircle}", ColorConstants.Yellow);
        }

        for (int spellLevel = fromCircle; spellLevel <= toCircle; spellLevel++)
        {
            int currentMaxSlots = CreaturePlugin.GetMaxSpellSlots(creature, classId, spellLevel);

            if (isPlayer)
            {
                player?.SendServerMessage($"[DEBUG]   Level {spellLevel}: current max slots = {currentMaxSlots}", ColorConstants.Yellow);
            }

            if (currentMaxSlots <= 0)
            {
                // To initialize spell structures for partial casters, we set remaining slots to 1.
                // This triggers the NWN engine to create the internal spell data structures needed
                // for AddKnownSpell to work. The actual spell slots will be properly managed by
                // PrestigeSpellSlotService through item properties on the creature hide.
                CreaturePlugin.SetRemainingSpellSlots(creature, classId, spellLevel, 1);

                if (isPlayer)
                {
                    player?.SendServerMessage($"[DEBUG]   Level {spellLevel}: INITIALIZED (set remaining=1)", ColorConstants.Cyan);
                }

                Log.Info($"      Initialized {classType} spell level {spellLevel} structure (CL {effectiveLevel})");
            }
            else if (isPlayer)
            {
                player?.SendServerMessage($"[DEBUG]   Level {spellLevel}: already has structure", ColorConstants.Yellow);
            }
        }
    }

    private int GrantClassSpells(NwCreature creature, ClassType classType, int classId, int fromCircle, int toCircle)
    {
        NwItem? pcKey = GetPcKey(creature);
        int spellsGranted = 0;
        bool isPlayer = creature.IsPlayerControlled(out NwPlayer? player);

        Log.Info($"    GrantClassSpells START: {classType}, classId={classId}, circles {fromCircle}-{toCircle}");

        if (isPlayer)
        {
            player?.SendServerMessage($"[DEBUG] GrantClassSpells START: {classType} circles {fromCircle}-{toCircle}", ColorConstants.Yellow);
        }

        // Ensure spell data structures exist for partial casters before adding spells
        EnsureSpellStructuresExist(creature, classType, classId, fromCircle, toCircle);

        for (int spellLevel = fromCircle; spellLevel <= toCircle; spellLevel++)
        {
            IReadOnlyList<int> spells = _spellCache.GetSpellsForClass(classType, spellLevel);

            Log.Info($"      Level {spellLevel}: {spells.Count} spells in cache");

            if (isPlayer)
            {
                player?.SendServerMessage($"[DEBUG]   Level {spellLevel}: {spells.Count} spells in cache", ColorConstants.Yellow);
            }

            if (spells.Count == 0)
            {
                Log.Warn($"      No spells found in cache for {classType} level {spellLevel}");
                if (isPlayer)
                {
                    player?.SendServerMessage($"[DEBUG]   Level {spellLevel}: No spells in cache!", ColorConstants.Orange);
                }
                continue;
            }

            int levelSpellsGranted = 0;
            foreach (int spellId in spells)
            {
                // Check if creature already knows this spell
                if (CreatureKnowsSpell(creature, classId, spellLevel, spellId))
                {
                    Log.Debug($"        Spell {spellId}: already known");
                    continue;
                }

                // CRITICAL: Log BEFORE adding spell
                Log.Info($"        About to add spell {spellId} (Level {spellLevel}) to {classType} (classId={classId})");

                // Add the spell
                CreaturePlugin.AddKnownSpell(creature, classId, spellLevel, spellId);
                spellsGranted++;
                levelSpellsGranted++;

                // CRITICAL: Verify spell was actually added
                bool stillKnown = CreatureKnowsSpell(creature, classId, spellLevel, spellId);
                Log.Info($"        After adding: CreatureKnowsSpell returned {stillKnown}");

                if (isPlayer && levelSpellsGranted <= 3) // Only show first 3 per level
                {
                    player?.SendServerMessage($"[DEBUG]   Spell {spellId}: added (verified={stillKnown})", ColorConstants.Cyan);
                }

                // Store on pcKey for persistence
                if (pcKey != null)
                {
                    string persistentKey = $"{PrestigeSpellPrefix}{classType}_{spellLevel}_{spellId}";
                    NWScript.SetLocalInt(pcKey, persistentKey, creature.Level);
                }

                Log.Debug($"      Added spell {spellId} (Level {spellLevel}) to {classType} spellbook");
            }

            Log.Info($"      Level {spellLevel}: {levelSpellsGranted}/{spells.Count} spells granted");
        }

        Log.Info($"    GrantClassSpells END: Total granted = {spellsGranted} from {fromCircle}-{toCircle}");

        if (spellsGranted > 0)
        {
            Log.Info($"    Granted {spellsGranted} {classType} spells for circles {fromCircle}-{toCircle}");
            if (isPlayer)
            {
                player?.SendServerMessage($"[DEBUG] GrantClassSpells: Total granted = {spellsGranted}", ColorConstants.Cyan);
            }
        }
        else if (isPlayer)
        {
            player?.SendServerMessage($"[DEBUG] GrantClassSpells: No spells granted!", ColorConstants.Orange);
        }

        return spellsGranted;
    }

    private int GrantDomainSpells(NwCreature creature, int clericClassId, int fromCircle, int toCircle)
    {
        // Get creature's domains
        int domain1 = NWScript.GetDomain(creature);
        int domain2 = NWScript.GetDomain(creature, 2);

        bool isPlayer = creature.IsPlayerControlled(out NwPlayer? player);

        if (isPlayer)
        {
            player?.SendServerMessage($"[DEBUG] GrantDomainSpells: Domain1={domain1}, Domain2={domain2}", ColorConstants.Yellow);
        }

        if (domain1 <= 0 && domain2 <= 0)
        {
            Log.Debug($"    No domains found for Cleric");
            if (isPlayer)
            {
                player?.SendServerMessage($"[DEBUG] No domains found, skipping domain spells", ColorConstants.Yellow);
            }
            return 0;
        }

        NwItem? pcKey = GetPcKey(creature);
        int domainSpellsGranted = 0;

        // Process each domain
        int[] domains = { domain1, domain2 };
        foreach (int domainId in domains)
        {
            if (domainId <= 0) continue;

            string domainName = _spellCache.GetDomainName(domainId);

            if (isPlayer)
            {
                player?.SendServerMessage($"[DEBUG] Processing domain {domainId} ({domainName})", ColorConstants.Yellow);
            }

            for (int spellLevel = fromCircle; spellLevel <= toCircle; spellLevel++)
            {
                int spellId = _spellCache.GetDomainSpell(domainId, spellLevel);

                if (spellId < 0) // No spell for this domain/level
                {
                    if (isPlayer)
                    {
                        player?.SendServerMessage($"[DEBUG]   Level {spellLevel}: No spell (spellId={spellId})", ColorConstants.Yellow);
                    }
                    continue;
                }

                if (isPlayer)
                {
                    player?.SendServerMessage($"[DEBUG]   Level {spellLevel}: spellId={spellId}", ColorConstants.Yellow);
                }

                // Check if creature already knows this spell
                if (CreatureKnowsSpell(creature, clericClassId, spellLevel, spellId))
                {
                    if (isPlayer)
                    {
                        player?.SendServerMessage($"[DEBUG]   Level {spellLevel}: already known, skipping", ColorConstants.Yellow);
                    }
                    continue;
                }

                // Add the spell
                if (isPlayer)
                {
                    player?.SendServerMessage($"[DEBUG]   Level {spellLevel}: adding...", ColorConstants.Cyan);
                }

                CreaturePlugin.AddKnownSpell(creature, clericClassId, spellLevel, spellId);
                domainSpellsGranted++;

                // Store on pcKey for persistence
                if (pcKey != null)
                {
                    string persistentKey = $"{PrestigeSpellPrefix}DOMAIN_{domainId}_{spellLevel}_{spellId}";
                    NWScript.SetLocalInt(pcKey, persistentKey, creature.Level);
                }

                Log.Debug($"      Added domain spell {spellId} (Level {spellLevel}) from {domainName}");
            }
        }

        if (domainSpellsGranted > 0)
        {
            Log.Info($"    Granted {domainSpellsGranted} domain spells for circles {fromCircle}-{toCircle}");
            if (isPlayer)
            {
                player?.SendServerMessage($"[DEBUG] GrantDomainSpells: Total granted = {domainSpellsGranted}", ColorConstants.Cyan);
            }
        }
        else if (isPlayer)
        {
            player?.SendServerMessage($"[DEBUG] GrantDomainSpells: No domain spells granted!", ColorConstants.Orange);
        }

        return domainSpellsGranted;
    }

    private bool CreatureKnowsSpell(NwCreature creature, int classId, int spellLevel, int spellId)
    {
        // Find the class info by class ID
        CreatureClassInfo? classInfo = creature.Classes.FirstOrDefault(c => c.Class.Id == classId);
        if (classInfo == null)
            return false;

        // Check if the spell is in the known spells list for this level
        IList<NwSpell>? knownSpells = classInfo.KnownSpells.ElementAtOrDefault(spellLevel);
        if (knownSpells == null)
            return false;

        return knownSpells.Any(s => s.Id == spellId);
    }

    private NwItem? GetPcKey(NwCreature creature)
    {
        return creature.Inventory.Items.FirstOrDefault(item => item.ResRef == "ds_pckey");
    }

    /// <summary>
    /// Restores prestige-granted divine spells from pcKey storage on login.
    /// Called after the initial spell grant check to ensure spells persist across logins.
    /// </summary>
    public void RestorePrestigeSpells(NwCreature creature)
    {
        NwItem? pcKey = GetPcKey(creature);
        if (pcKey == null)
        {
            Log.Debug($"No ds_pckey found for {creature.Name}, skipping spell restoration");
            return;
        }

        int spellsRestored = 0;

        // Check each divine class the creature has
        foreach (ClassType classType in DivineCasterClasses)
        {
            CreatureClassInfo? classInfo = creature.GetClassInfo(classType);
            if (classInfo == null || classInfo.Level == 0)
                continue;

            int classId = classInfo.Class.Id;

            // Scan pcKey local variables for prestige spells
            string classPrefix = $"{PrestigeSpellPrefix}{classType}_";

            foreach (ObjectVariable localVar in pcKey.LocalVariables)
            {
                if (!localVar.Name.StartsWith(classPrefix))
                    continue;

                // Parse: DIVINE_PRESTIGE_SPELL_{classType}_{spellLevel}_{spellId}
                string remainder = localVar.Name.Substring(classPrefix.Length);
                string[] parts = remainder.Split('_');

                if (parts.Length != 2)
                    continue;

                if (!int.TryParse(parts[0], out int spellLevel) ||
                    !int.TryParse(parts[1], out int spellId))
                    continue;

                // Check if creature still qualifies (level stored >= current level means it should be kept)
                int grantedAtLevel = pcKey.GetObjectVariable<LocalVariableInt>(localVar.Name).Value;
                if (grantedAtLevel > creature.Level)
                {
                    // They've deleveled below when this was granted - remove the storage
                    pcKey.GetObjectVariable<LocalVariableInt>(localVar.Name).Delete();
                    continue;
                }

                // Restore the spell if not already known
                if (!CreatureKnowsSpell(creature, classId, spellLevel, spellId))
                {
                    CreaturePlugin.AddKnownSpell(creature, classId, spellLevel, spellId);
                    spellsRestored++;
                    Log.Debug($"Restored spell {spellId} (Level {spellLevel}) to {creature.Name}'s {classType} spellbook");
                }
            }
        }

        // Also restore domain spells
        string domainPrefix = $"{PrestigeSpellPrefix}DOMAIN_";
        CreatureClassInfo? clericInfo = creature.GetClassInfo(ClassType.Cleric);

        if (clericInfo != null && clericInfo.Level > 0)
        {
            int clericClassId = clericInfo.Class.Id;

            foreach (ObjectVariable localVar in pcKey.LocalVariables)
            {
                if (!localVar.Name.StartsWith(domainPrefix))
                    continue;

                // Parse: DIVINE_PRESTIGE_SPELL_DOMAIN_{domainId}_{spellLevel}_{spellId}
                string remainder = localVar.Name.Substring(domainPrefix.Length);
                string[] parts = remainder.Split('_');

                if (parts.Length != 3)
                    continue;

                if (!int.TryParse(parts[0], out int domainId) ||
                    !int.TryParse(parts[1], out int spellLevel) ||
                    !int.TryParse(parts[2], out int spellId))
                    continue;

                // Check if creature still has this domain
                int domain1 = NWScript.GetDomain(creature);
                int domain2 = NWScript.GetDomain(creature, 2);

                if (domainId != domain1 && domainId != domain2)
                {
                    // Domain changed - remove the storage
                    pcKey.GetObjectVariable<LocalVariableInt>(localVar.Name).Delete();
                    continue;
                }

                // Check level qualification
                int grantedAtLevel = pcKey.GetObjectVariable<LocalVariableInt>(localVar.Name).Value;
                if (grantedAtLevel > creature.Level)
                {
                    pcKey.GetObjectVariable<LocalVariableInt>(localVar.Name).Delete();
                    continue;
                }

                // Restore the spell if not already known
                if (!CreatureKnowsSpell(creature, clericClassId, spellLevel, spellId))
                {
                    CreaturePlugin.AddKnownSpell(creature, clericClassId, spellLevel, spellId);
                    spellsRestored++;
                    Log.Debug($"Restored domain spell {spellId} (Level {spellLevel}) to {creature.Name}'s Cleric spellbook");
                }
            }
        }

        if (spellsRestored > 0)
        {
            Log.Info($"Restored {spellsRestored} prestige divine spells for {creature.Name}");
        }
    }
}





