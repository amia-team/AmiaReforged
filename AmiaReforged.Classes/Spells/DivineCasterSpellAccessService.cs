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
        _spellCache = spellCache;

        // Subscribe to events
        NwModule.Instance.OnClientEnter += OnClientEnter;
        eventService.SubscribeAll<OnLevelUp, OnLevelUp.Factory>(OnLevelUp, EventCallbackType.After);
        eventService.SubscribeAll<OnLevelDown, OnLevelDown.Factory>(OnLevelDown, EventCallbackType.After);

        Log.Info("Divine Caster Spell Access Service initialized.");
    }

    private void OnClientEnter(ModuleEvents.OnClientEnter obj)
    {
        if (obj.Player.IsDM) return;
        if (obj.Player.LoginCreature == null) return;

        // Small delay to ensure character is fully loaded
        NwTask.Run(async () =>
        {
            await NwTask.Delay(TimeSpan.FromMilliseconds(500));
            await NwTask.SwitchToMainThread();

            if (obj.Player.LoginCreature?.IsValid == true)
            {
                ProcessCreatureSpellAccess(obj.Player.LoginCreature, isLogin: true);
            }
        });
    }

    private void OnLevelUp(OnLevelUp obj)
    {
        if (!obj.Creature.IsPlayerControlled(out _)) return;
        ProcessCreatureSpellAccess(obj.Creature, isLogin: false);
    }

    private void OnLevelDown(OnLevelDown obj)
    {
        if (!obj.Creature.IsPlayerControlled(out _)) return;
        // On level down, we don't add new spells, but NWN should naturally remove
        // spells granted at higher levels through AddKnownSpell/AddFeatByLevel mechanism
        // We just need to recalculate in case they still qualify for some spells
        ProcessCreatureSpellAccess(obj.Creature, isLogin: false);
    }

    private void ProcessCreatureSpellAccess(NwCreature creature, bool isLogin)
    {
        Log.Info($"Processing divine spell access for {creature.Name} (isLogin={isLogin})");

        // Get effective caster levels for all classes
        var effectiveLevels = EffectiveCasterLevelCalculator.CalculateAllEffectiveCasterLevels(creature);

        // Process each divine caster class the creature has
        foreach (ClassType classType in DivineCasterClasses)
        {
            CreatureClassInfo? classInfo = creature.GetClassInfo(classType);
            if (classInfo == null || classInfo.Level == 0)
                continue;

            int actualLevel = classInfo.Level;
            int effectiveLevel = effectiveLevels.TryGetValue(classType, out int el) ? el : actualLevel;

            // Only process if effective level is higher than actual level
            if (effectiveLevel <= actualLevel)
            {
                Log.Debug($"  {classType}: Effective CL ({effectiveLevel}) <= Actual Level ({actualLevel}), no bonus spells needed");
                continue;
            }

            Log.Info($"  {classType}: Actual Level {actualLevel}, Effective CL {effectiveLevel}");

            // Calculate spell circle access
            int actualMaxCircle = DivineSpellProgressionData.GetMaxSpellCircleForCasterLevel(classType, actualLevel);
            int effectiveMaxCircle = DivineSpellProgressionData.GetMaxSpellCircleForCasterLevel(classType, effectiveLevel);

            if (effectiveMaxCircle <= actualMaxCircle)
            {
                Log.Debug($"    Max circle unchanged ({actualMaxCircle}), no new spells to grant");
                continue;
            }

            Log.Info($"    Granting access to circles {actualMaxCircle + 1}-{effectiveMaxCircle} (was {actualMaxCircle}, now {effectiveMaxCircle})");

            // Grant class spells for newly accessible circles
            GrantClassSpells(creature, classType, classInfo.Class.Id, actualMaxCircle + 1, effectiveMaxCircle);

            // For Clerics, also grant domain spells
            if (classType == ClassType.Cleric)
            {
                GrantDomainSpells(creature, classInfo.Class.Id, actualMaxCircle + 1, effectiveMaxCircle);
            }
        }
    }

    private void GrantClassSpells(NwCreature creature, ClassType classType, int classId, int fromCircle, int toCircle)
    {
        NwItem? pcKey = GetPcKey(creature);
        int spellsGranted = 0;

        for (int spellLevel = fromCircle; spellLevel <= toCircle; spellLevel++)
        {
            var spells = _spellCache.GetSpellsForClass(classType, spellLevel);

            foreach (int spellId in spells)
            {
                // Check if creature already knows this spell
                if (CreatureKnowsSpell(creature, classId, spellLevel, spellId))
                    continue;

                // Add the spell
                CreaturePlugin.AddKnownSpell(creature, classId, spellLevel, spellId);
                spellsGranted++;

                // Store on pcKey for persistence
                if (pcKey != null)
                {
                    string persistentKey = $"{PrestigeSpellPrefix}{classType}_{spellLevel}_{spellId}";
                    NWScript.SetLocalInt(pcKey, persistentKey, creature.Level);
                }

                Log.Debug($"      Added spell {spellId} (Level {spellLevel}) to {classType} spellbook");
            }
        }

        if (spellsGranted > 0)
        {
            Log.Info($"    Granted {spellsGranted} {classType} spells for circles {fromCircle}-{toCircle}");
        }
    }

    private void GrantDomainSpells(NwCreature creature, int clericClassId, int fromCircle, int toCircle)
    {
        // Get creature's domains
        int domain1 = NWScript.GetDomain(creature);
        int domain2 = NWScript.GetDomain(creature, 2);

        if (domain1 <= 0 && domain2 <= 0)
        {
            Log.Debug($"    No domains found for Cleric");
            return;
        }

        NwItem? pcKey = GetPcKey(creature);
        int domainSpellsGranted = 0;

        // Process each domain
        int[] domains = { domain1, domain2 };
        foreach (int domainId in domains)
        {
            if (domainId <= 0) continue;

            string domainName = _spellCache.GetDomainName(domainId);

            for (int spellLevel = fromCircle; spellLevel <= toCircle; spellLevel++)
            {
                int spellId = _spellCache.GetDomainSpell(domainId, spellLevel);

                if (spellId < 0) // No spell for this domain/level
                    continue;

                // Check if creature already knows this spell
                if (CreatureKnowsSpell(creature, clericClassId, spellLevel, spellId))
                    continue;

                // Add the spell
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
        }
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

            foreach (var localVar in pcKey.LocalVariables)
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

            foreach (var localVar in pcKey.LocalVariables)
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





