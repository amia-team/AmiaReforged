using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;
using NLog;

namespace AmiaReforged.Classes.Spells;

/// <summary>
/// Grants bonus spell slots to characters with prestige caster classes via creature hide item properties.
/// This works around the NWN engine limitation where caster level overrides don't affect spell slot quantity.
/// The bonus slots are applied through item properties on the character's creature hide (skin),
/// which is invisible to players and cannot be dropped or traded.
/// </summary>
[ServiceBinding(typeof(PrestigeSpellSlotService))]
public class PrestigeSpellSlotService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    // Track creatures currently being processed to prevent concurrent execution
    private readonly HashSet<uint> _processingCreatures = new();
    private readonly object _processingLock = new();

    // Dictionary mapping prestige classes to their caster level modifier formulas
    private readonly Dictionary<ClassType, Func<int, int>> _prestigeClassModifiers = new()
    {
        { ClassType.DragonDisciple, prcLevel => Math.Max(0, prcLevel) },
        { ClassType.Blackguard, prcLevel => Math.Max(0, prcLevel) },
        { ClassType.PaleMaster, prcLevel => Math.Max(0, prcLevel) },
        { ClassType.ArcaneArcher, prcLevel => Math.Max(0, prcLevel / 2) }
    };

    // Mapping of prestige classes to their valid base caster classes
    // NOTE: Only Dragon Disciple and Blackguard need custom spell slot handling
    // Other prestige classes (Pale Master, Arcane Archer) are handled correctly by Classes.2da
    private static readonly Dictionary<ClassType, HashSet<ClassType>> PrestigeToBaseCasterMap = new()
    {
        {
            ClassType.DragonDisciple,
            new HashSet<ClassType> { ClassType.Sorcerer, ClassType.Bard }
        },
        {
            ClassType.Blackguard,
            new HashSet<ClassType> { ClassType.Cleric, ClassType.Druid, ClassType.Ranger }
        }
    };

    public PrestigeSpellSlotService(EventService eventService)
    {
        // Update creature hide when character enters the server
        NwModule.Instance.OnClientEnter += OnClientEnter;

        // Update creature hide when character levels up or down
        eventService.SubscribeAll<OnLevelUp, OnLevelUp.Factory>(OnLevelUp, EventCallbackType.After);
        eventService.SubscribeAll<OnLevelDown, OnLevelDown.Factory>(OnLevelDown, EventCallbackType.After);
    }

    private void OnClientEnter(ModuleEvents.OnClientEnter obj)
    {
        if (obj.Player.LoginCreature == null) return;
        NwTask.Run(async () => await ApplyPrestigeSpellSlotSkin(obj.Player.LoginCreature));
    }

    private void OnLevelUp(OnLevelUp obj)
    {
        NwTask.Run(async () => await ApplyPrestigeSpellSlotSkin(obj.Creature));
    }

    private void OnLevelDown(OnLevelDown obj)
    {
        NwTask.Run(async () => await ApplyPrestigeSpellSlotSkin(obj.Creature));
    }

    private async Task ApplyPrestigeSpellSlotSkin(NwCreature creature)
    {
        // Prevent concurrent execution for the same creature
        lock (_processingLock)
        {
            if (_processingCreatures.Contains(creature))
            {
                Log.Info($"=== SKIPPED: ApplyPrestigeSpellSlotSkin already in progress for {creature.Name} ===");
                return;
            }
            _processingCreatures.Add(creature);
        }

        try
        {
            await ApplyPrestigeSpellSlotSkinInternal(creature);
        }
        finally
        {
            lock (_processingLock)
            {
                _processingCreatures.Remove(creature);
            }
        }
    }

    private async Task ApplyPrestigeSpellSlotSkinInternal(NwCreature creature)
    {
        Log.Info($"=== STEP 0: Starting ApplyPrestigeSpellSlotSkin for {creature.Name} ===");

        // STEP 1: Look for equipped creature hide
        Log.Info($"STEP 1: Checking for equipped creature hide in skin slot...");
        NwItem? existingHide = creature.GetItemInSlot(InventorySlot.CreatureSkin);

        if (existingHide != null)
        {
            Log.Info($"STEP 1: ✓ Found equipped hide: Name='{existingHide.Name}', ResRef='{existingHide.ResRef}'");
        }
        else
        {
            Log.Info($"STEP 1: ✗ No creature hide found in skin slot");
        }

        // STEP 2: Destroy existing hide if it exists
        if (existingHide != null)
        {
            Log.Info($"STEP 2: Destroying existing hide...");
            existingHide.Destroy();
            await NwTask.NextFrame();
            Log.Info($"STEP 2: ✓ Existing hide destroyed");
        }
        else
        {
            Log.Info($"STEP 2: No hide to destroy (none existed)");
        }

        // Clean up any duplicate hides in inventory
        List<NwItem> hideInInventory = creature.Inventory.Items
            .Where(item => item.ResRef == "ds_pchide")
            .ToList();

        if (hideInInventory.Count > 0)
        {
            Log.Info($"STEP 2b: Found {hideInInventory.Count} duplicate hide(s) in inventory - removing them");
            foreach (NwItem hide in hideInInventory)
            {
                hide.Destroy();
            }
            Log.Info($"STEP 2b: ✓ Inventory duplicates removed");
        }

        // STEP 3: Analyze character classes BEFORE creating hide
        Log.Info($"STEP 3: Analyzing character classes...");
        List<(ClassType classType, int level)> prestigeClasses = [];
        Dictionary<ClassType, int> allBaseClasses = new();

        foreach (CreatureClassInfo charClass in creature.Classes)
        {
            Log.Info($"  - Class: {charClass.Class.Name} ({charClass.Class.ClassType}) Level {charClass.Level}");

            if (_prestigeClassModifiers.ContainsKey(charClass.Class.ClassType))
            {
                prestigeClasses.Add((charClass.Class.ClassType, charClass.Level));
                Log.Info($"    ✓ This is a prestige caster class!");
            }

            // Track all base classes that could potentially be boosted
            HashSet<ClassType> allValidBaseClasses = PrestigeToBaseCasterMap.Values
                .SelectMany(set => set)
                .ToHashSet();

            if (allValidBaseClasses.Contains(charClass.Class.ClassType))
            {
                allBaseClasses[charClass.Class.ClassType] = charClass.Level;
                Log.Info($"    ✓ This is a potential base caster class!");
            }
        }

        // Check if we need to add spell slots - ONLY if they have DD or Blackguard
        if (prestigeClasses.Count == 0)
        {
            Log.Info($"STEP 3: No prestige caster classes (Dragon Disciple/Blackguard) found - no hide needed");
            Log.Info($"=== FINISHED: No hide needed for {creature.Name} ===");
            return;
        }
        else if (allBaseClasses.Count == 0)
        {
            Log.Info($"STEP 3: Has prestige class but no valid base caster classes - no hide needed");
            Log.Info($"=== FINISHED: No hide needed for {creature.Name} ===");
            return;
        }
        else
        {
            Log.Info($"STEP 3: ✓ Found {prestigeClasses.Count} prestige caster class(es) and {allBaseClasses.Count} base caster class(es)");
        }

        // STEP 3b: Create new creature hide (only if needed)
        Log.Info($"STEP 3b: Creating new creature hide...");
        NwItem? creatureHide = await NwItem.Create("ds_pchide", creature);

        if (creatureHide == null)
        {
            Log.Error($"STEP 3b: ✗✗✗ FAILED to create creature hide!");
            return;
        }

        Log.Info($"STEP 3b: ✓ Creature hide created successfully - Name='{creatureHide.Name}', ResRef='{creatureHide.ResRef}'");
        creatureHide.Droppable = false;
        Log.Info($"STEP 3b: ✓ Set hide as non-droppable");

        // STEP 4: Add appropriate spell slot bonuses
        Log.Info($"STEP 4: Determining which base class(es) to boost...");

        // Dictionary to track bonuses per base class: BaseClass -> (ActualLevel, TotalModifier)
        Dictionary<ClassType, (int actualLevel, int modifier)> baseClassBonuses = new();

        foreach ((ClassType prcType, int prcLevel) in prestigeClasses)
        {
            // Get valid base classes for this prestige class
            if (!PrestigeToBaseCasterMap.TryGetValue(prcType, out HashSet<ClassType>? validBaseClasses))
            {
                Log.Info($"  - Prestige class {prcType} has no valid base classes configured");
                continue;
            }

            Log.Info($"  - Processing prestige class {prcType} Level {prcLevel}");
            Log.Info($"    Valid base classes: {string.Join(", ", validBaseClasses)}");

            // Find the highest level valid base class for THIS prestige class
            ClassType? selectedBaseForThisPrc = null;
            int highestLevelForThisPrc = 0;

            foreach (var kvp in allBaseClasses)
            {
                if (validBaseClasses.Contains(kvp.Key))
                {
                    Log.Info($"    Found valid base class: {kvp.Key} Level {kvp.Value}");

                    if (kvp.Value > highestLevelForThisPrc)
                    {
                        selectedBaseForThisPrc = kvp.Key;
                        highestLevelForThisPrc = kvp.Value;
                        Log.Info($"    ✓ This is the highest valid base class for {prcType}");
                    }
                }
            }

            if (selectedBaseForThisPrc == null)
            {
                Log.Info($"    ✗ No valid base class found for {prcType} - skipping");
                continue;
            }

            // Add this prestige class's bonus to the selected base class
            int modifier = _prestigeClassModifiers[prcType](prcLevel);

            if (baseClassBonuses.ContainsKey(selectedBaseForThisPrc.Value))
            {
                // If this base class already has bonuses, add to them
                var existing = baseClassBonuses[selectedBaseForThisPrc.Value];
                baseClassBonuses[selectedBaseForThisPrc.Value] = (existing.actualLevel, existing.modifier + modifier);
                Log.Info($"  - {prcType} Level {prcLevel} adds +{modifier} to {selectedBaseForThisPrc} (now +{existing.modifier + modifier} total)");
            }
            else
            {
                // First bonus for this base class
                baseClassBonuses[selectedBaseForThisPrc.Value] = (highestLevelForThisPrc, modifier);
                Log.Info($"  - {prcType} Level {prcLevel} adds +{modifier} to {selectedBaseForThisPrc}");
            }
        }

        if (baseClassBonuses.Count == 0)
        {
            Log.Info($"STEP 4: No valid base classes found for any prestige class - cannot add bonuses");
            creatureHide.Destroy();
            Log.Info($"STEP 4: Destroying hide that was created (no valid base classes)");
            Log.Info($"=== FINISHED: No hide needed for {creature.Name} ===");
            return;
        }

        // Apply bonuses for each base class
        Log.Info($"STEP 4: Applying bonuses to {baseClassBonuses.Count} base class(es)...");

        foreach (var kvp in baseClassBonuses)
        {
            ClassType baseClass = kvp.Key;
            int actualLevel = kvp.Value.actualLevel;
            int modifier = kvp.Value.modifier;
            int effectiveCasterLevel = Math.Max(1, actualLevel + modifier);

            Log.Info($"  - Base class: {baseClass} Level {actualLevel}");
            Log.Info($"    Effective caster level: {effectiveCasterLevel} (Base {actualLevel} + Prestige {modifier})");

            AddPrestigeSpellSlotProperties(creatureHide, creature, baseClass, actualLevel, effectiveCasterLevel);
            Log.Info($"    ✓ Spell slot bonuses added for {baseClass}");
        }

        Log.Info($"STEP 4: ✓ All spell slot bonuses applied");

        // STEP 5: Equip the creature hide and verify
        Log.Info($"STEP 5: Equipping creature hide to skin slot...");
        await NwTask.NextFrame();
        creature.RunEquip(creatureHide, InventorySlot.CreatureSkin);
        await NwTask.NextFrame();

        NwItem? equippedHide = creature.GetItemInSlot(InventorySlot.CreatureSkin);

        if (equippedHide != null && equippedHide == creatureHide)
        {
            Log.Info($"STEP 5: ✓✓✓ SUCCESS - Creature hide equipped and verified!");
            Log.Info($"STEP 5: Equipped hide details - Name='{equippedHide.Name}', ResRef='{equippedHide.ResRef}', Property Count={equippedHide.ItemProperties.Count()}");
        }
        else if (equippedHide != null)
        {
            Log.Error($"STEP 5: ✗ WRONG ITEM EQUIPPED - Expected our hide but found: Name='{equippedHide.Name}', ResRef='{equippedHide.ResRef}'");
        }
        else
        {
            Log.Error($"STEP 5: ✗✗✗ FAILED - Nothing equipped in skin slot!");
        }

        // STEP 6: Schedule a delayed verification check to ensure hide stays equipped
        Log.Info($"STEP 6: Scheduling delayed verification check...");
        _ = NwTask.Run(async () => await VerifyHideEquipped(creature));

        Log.Info($"=== FINISHED: ApplyPrestigeSpellSlotSkin for {creature.Name} ===");
    }

    /// <summary>
    /// Delayed verification to ensure hide is properly equipped and not in inventory.
    /// Runs after a short delay to catch edge cases where the hide doesn't stay equipped.
    /// </summary>
    private async Task VerifyHideEquipped(NwCreature creature)
    {
        // Wait 2 seconds to let everything settle
        await NwTask.Delay(TimeSpan.FromSeconds(2));

        // Check if creature is still valid
        if (!creature.IsValid)
        {
            Log.Info($"VERIFICATION: Creature no longer valid, skipping verification");
            return;
        }

        Log.Info($"=== VERIFICATION CHECK: Starting for {creature.Name} ===");

        // Check what's in the skin slot
        NwItem? equippedHide = creature.GetItemInSlot(InventorySlot.CreatureSkin);

        // Check for any PCHIDE items in inventory
        List<NwItem> hidesInInventory = creature.Inventory.Items
            .Where(item => item.ResRef == "ds_pchide")
            .ToList();

        if (hidesInInventory.Count > 0)
        {
            Log.Warn($"VERIFICATION: Found {hidesInInventory.Count} PCHIDE item(s) in inventory - this should not happen!");

            // If there's a hide properly equipped, destroy the inventory duplicates
            if (equippedHide != null && equippedHide.ResRef == "ds_pchide")
            {
                Log.Info($"VERIFICATION: Hide is properly equipped, removing inventory duplicates");
                foreach (NwItem hide in hidesInInventory)
                {
                    Log.Info($"VERIFICATION: Destroying duplicate hide in inventory");
                    hide.Destroy();
                }
                Log.Info($"VERIFICATION: ✓ Cleaned up {hidesInInventory.Count} duplicate(s)");
            }
            else
            {
                // No hide equipped, but there are hides in inventory - equip one and destroy the rest
                Log.Warn($"VERIFICATION: No hide equipped but found hide(s) in inventory - attempting to equip");

                NwItem firstHide = hidesInInventory[0];
                creature.RunEquip(firstHide, InventorySlot.CreatureSkin);
                await NwTask.NextFrame();

                // Destroy any remaining duplicates
                for (int i = 1; i < hidesInInventory.Count; i++)
                {
                    hidesInInventory[i].Destroy();
                }

                // Verify it got equipped
                NwItem? nowEquipped = creature.GetItemInSlot(InventorySlot.CreatureSkin);
                if (nowEquipped != null && nowEquipped == firstHide)
                {
                    Log.Info($"VERIFICATION: ✓ Successfully equipped hide from inventory");
                }
                else
                {
                    Log.Error($"VERIFICATION: ✗ Failed to equip hide from inventory - this may require manual intervention");
                }
            }
        }
        else if (equippedHide == null || equippedHide.ResRef != "ds_pchide")
        {
            // No hide in inventory and none equipped - character needs one, re-apply
            Log.Warn($"VERIFICATION: No PCHIDE found equipped or in inventory - re-applying");
            await ApplyPrestigeSpellSlotSkin(creature);
        }
        else
        {
            // Everything is fine
            Log.Info($"VERIFICATION: ✓ Hide is properly equipped and no duplicates in inventory");
        }

        Log.Info($"=== VERIFICATION CHECK: Finished for {creature.Name} ===");
    }

    private void AddPrestigeSpellSlotProperties(NwItem item, NwCreature creature, ClassType baseClass, int actualLevel, int effectiveLevel)
    {
        Log.Info($"  === BEGIN AddPrestigeSpellSlotProperties ===");

        // Calculate how many bonus slots we need for each spell level
        int abilityModifier = GetAbilityModifier(creature, baseClass);
        Log.Info($"  Ability modifier for {baseClass}: {abilityModifier}");

        // Convert ClassType to NWScript class constant
        int classConst = GetClassConstant(baseClass);
        Log.Info($"  Class constant for {baseClass}: {classConst}");

        int totalPropertiesAdded = 0;

        for (int spellLevel = 0; spellLevel <= 9; spellLevel++)
        {
            // Calculate slots at actual level vs effective level
            int actualBaseSlots = GetBaseSpellSlotsForLevel(baseClass, actualLevel, spellLevel);
            int actualBonusSlots = GetBonusSpellSlots(abilityModifier, spellLevel);
            int actualTotal = actualBaseSlots + actualBonusSlots;

            int effectiveBaseSlots = GetBaseSpellSlotsForLevel(baseClass, effectiveLevel, spellLevel);
            int effectiveBonusSlots = GetBonusSpellSlots(abilityModifier, spellLevel);
            int effectiveTotal = effectiveBaseSlots + effectiveBonusSlots;

            // The difference is how many bonus slots we need to add
            int bonusSlotsNeeded = Math.Max(0, effectiveTotal - actualTotal);

            Log.Info($"  Spell Level {spellLevel}:");
            Log.Info($"    Actual Level {actualLevel}: {actualTotal} slots ({actualBaseSlots} base + {actualBonusSlots} ability bonus)");
            Log.Info($"    Effective Level {effectiveLevel}: {effectiveTotal} slots ({effectiveBaseSlots} base + {effectiveBonusSlots} ability bonus)");
            Log.Info($"    Difference: {bonusSlotsNeeded} bonus slot(s) needed");

            if (bonusSlotsNeeded > 0)
            {
                Log.Info($"    Adding {bonusSlotsNeeded} bonus spell slot propert(ies)...");

                // Add the property multiple times for the number of bonus slots needed
                for (int i = 0; i < bonusSlotsNeeded; i++)
                {
                    // Create and add the bonus spell slot property
                    ItemProperty bonusSlotProperty = NWScript.ItemPropertyBonusLevelSpell(classConst, spellLevel)!;
                    item.AddItemProperty(bonusSlotProperty, EffectDuration.Permanent);
                    totalPropertiesAdded++;
                }

                Log.Info($"    ✓ Added {bonusSlotsNeeded} bonus level {spellLevel} spell slot(s) for {baseClass}");
            }
            else
            {
                Log.Info($"    No bonus slots needed for this level");
            }
        }

        Log.Info($"  === END AddPrestigeSpellSlotProperties - Total properties added: {totalPropertiesAdded} ===");
    }

    private int GetClassConstant(ClassType classType)
    {
        return classType switch
        {
            ClassType.Wizard => NWScript.IP_CONST_CLASS_WIZARD,
            ClassType.Sorcerer => NWScript.IP_CONST_CLASS_SORCERER,
            ClassType.Bard => NWScript.IP_CONST_CLASS_BARD,
            ClassType.Cleric => NWScript.IP_CONST_CLASS_CLERIC,
            ClassType.Druid => NWScript.IP_CONST_CLASS_DRUID,
            ClassType.Paladin => NWScript.IP_CONST_CLASS_PALADIN,
            ClassType.Ranger => NWScript.IP_CONST_CLASS_RANGER,
            _ => (int)classType
        };
    }

    private int GetAbilityModifier(NwCreature creature, ClassType classType)
    {
        Ability ability = classType switch
        {
            ClassType.Wizard => Ability.Intelligence,
            ClassType.Sorcerer => Ability.Charisma,
            ClassType.Bard => Ability.Charisma,
            ClassType.Cleric => Ability.Wisdom,
            ClassType.Druid => Ability.Wisdom,
            ClassType.Paladin => Ability.Wisdom,
            ClassType.Ranger => Ability.Wisdom,
            _ => Ability.Intelligence
        };

        return creature.GetAbilityModifier(ability);
    }

    private int GetBonusSpellSlots(int abilityModifier, int spellLevel)
    {
        // Standard D&D 3.5 / NWN bonus spell formula
        // You get bonus spells if your ability modifier is high enough
        if (abilityModifier < spellLevel) return 0;

        // In NWN, bonus spells = (ability mod - spell level + 1) / 4, minimum 1 if you qualify
        return Math.Max(1, (abilityModifier - spellLevel + 1) / 4);
    }

    private int GetBaseSpellSlotsForLevel(ClassType classType, int casterLevel, int spellLevel)
    {
        return classType switch
        {
            ClassType.Bard => GetBardSpellSlots(casterLevel, spellLevel),
            ClassType.Sorcerer => GetSorcererSpellSlots(casterLevel, spellLevel),
            ClassType.Wizard => GetWizardSpellSlots(casterLevel, spellLevel),
            ClassType.Cleric => GetClericSpellSlots(casterLevel, spellLevel),
            ClassType.Druid => GetDruidSpellSlots(casterLevel, spellLevel),
            ClassType.Paladin => GetPaladinSpellSlots(casterLevel, spellLevel),
            ClassType.Ranger => GetRangerSpellSlots(casterLevel, spellLevel),
            _ => 0
        };
    }

    // NWN spell slot progression tables
    private int GetBardSpellSlots(int casterLevel, int spellLevel)
    {
        // Bard spell progression per cls_spgn_bard.2da
        return (spellLevel, casterLevel) switch
        {
            // Level 0 (cantrips) - 2 at L1, 3 at L2-13, 4 at L14+
            (0, >= 14) => 4,
            (0, >= 2) => 3,
            (0, >= 1) => 2,

            // Level 1 - 0 at L1-2, 1 at L3, 2 at L4, 3 at L5-12, 4 at L13+
            (1, >= 13) => 4,
            (1, >= 5) => 3,
            (1, >= 4) => 2,
            (1, >= 3) => 1,

            // Level 2 - 0 at L1-4, 1 at L5, 2 at L6-14, 4 at L15+
            (2, >= 15) => 4,
            (2, >= 6) => 2,
            (2, >= 5) => 1,

            // Level 3 - 0 at L1-7, 1 at L8, 2 at L9-13, 3 at L14, 4 at L16+
            (3, >= 16) => 4,
            (3, >= 14) => 3,
            (3, >= 9) => 2,
            (3, >= 8) => 1,

            // Level 4 - 0 at L1-10, 1 at L11, 2 at L12-13, 3 at L14-16, 4 at L17+
            (4, >= 17) => 4,
            (4, >= 14) => 3,
            (4, >= 12) => 2,
            (4, >= 11) => 1,

            // Level 5 - 0 at L1-13, 1 at L14, 2 at L15-16, 3 at L17, 4 at L18+
            (5, >= 18) => 4,
            (5, >= 17) => 3,
            (5, >= 15) => 2,
            (5, >= 14) => 1,

            // Level 6 - 0 at L1-16, 1 at L17, 2 at L18, 3 at L19, 4 at L20+
            (6, >= 20) => 4,
            (6, >= 19) => 3,
            (6, >= 18) => 2,
            (6, >= 17) => 1,

            _ => 0
        };
    }

    private int GetSorcererSpellSlots(int casterLevel, int spellLevel)
    {
        // Sorcerer spell progression per cls_spgn_sorc.2da
        return (spellLevel, casterLevel) switch
        {
            // Level 0 - 5 at L1, 6 at L2+
            (0, >= 2) => 6,
            (0, >= 1) => 5,

            // Level 1 - 3 at L1, 4 at L2, 5 at L3, 6 at L4+
            (1, >= 4) => 6,
            (1, >= 3) => 5,
            (1, >= 2) => 4,
            (1, >= 1) => 3,

            // Level 2 - 0 at L1-3, 3 at L4, 4 at L5, 5 at L6, 6 at L7+
            (2, >= 7) => 6,
            (2, >= 6) => 5,
            (2, >= 5) => 4,
            (2, >= 4) => 3,

            // Level 3 - 0 at L1-5, 3 at L6, 4 at L7, 5 at L8, 6 at L9+
            (3, >= 9) => 6,
            (3, >= 8) => 5,
            (3, >= 7) => 4,
            (3, >= 6) => 3,

            // Level 4 - 0 at L1-7, 3 at L8, 4 at L9, 5 at L10, 6 at L11+
            (4, >= 11) => 6,
            (4, >= 10) => 5,
            (4, >= 9) => 4,
            (4, >= 8) => 3,

            // Level 5 - 0 at L1-9, 3 at L10, 4 at L11, 5 at L12, 6 at L13+
            (5, >= 13) => 6,
            (5, >= 12) => 5,
            (5, >= 11) => 4,
            (5, >= 10) => 3,

            // Level 6 - 0 at L1-11, 3 at L12, 4 at L13, 5 at L14, 6 at L15+
            (6, >= 15) => 6,
            (6, >= 14) => 5,
            (6, >= 13) => 4,
            (6, >= 12) => 3,

            // Level 7 - 0 at L1-13, 3 at L14, 4 at L15, 5 at L16, 6 at L17+
            (7, >= 17) => 6,
            (7, >= 16) => 5,
            (7, >= 15) => 4,
            (7, >= 14) => 3,

            // Level 8 - 0 at L1-15, 3 at L16, 4 at L17, 5 at L18, 6 at L19+
            (8, >= 19) => 6,
            (8, >= 18) => 5,
            (8, >= 17) => 4,
            (8, >= 16) => 3,

            // Level 9 - 0 at L1-17, 3 at L18, 4 at L19, 6 at L20+
            (9, >= 20) => 6,
            (9, >= 19) => 4,
            (9, >= 18) => 3,

            _ => 0
        };
    }

    private int GetWizardSpellSlots(int casterLevel, int spellLevel)
    {
        // Wizard spell progression per cls_spgn_wiz.2da
        return (spellLevel, casterLevel) switch
        {
            // Level 0 - 3 at L1, 4 at L2+
            (0, >= 2) => 4,
            (0, >= 1) => 3,

            // Level 1 - 1 at L1, 2 at L2-3, 3 at L4-6, 4 at L7+
            (1, >= 7) => 4,
            (1, >= 4) => 3,
            (1, >= 2) => 2,
            (1, >= 1) => 1,

            // Level 2 - 0 at L1-2, 1 at L3, 2 at L4-5, 3 at L6-8, 4 at L9+
            (2, >= 9) => 4,
            (2, >= 6) => 3,
            (2, >= 4) => 2,
            (2, >= 3) => 1,

            // Level 3 - 0 at L1-4, 1 at L5, 2 at L6-7, 3 at L8-10, 4 at L11+
            (3, >= 11) => 4,
            (3, >= 8) => 3,
            (3, >= 6) => 2,
            (3, >= 5) => 1,

            // Level 4 - 0 at L1-6, 1 at L7, 2 at L8-10, 3 at L11-12, 4 at L13+
            (4, >= 13) => 4,
            (4, >= 11) => 3,
            (4, >= 8) => 2,
            (4, >= 7) => 1,

            // Level 5 - 0 at L1-8, 1 at L9, 2 at L10-11, 3 at L12-14, 4 at L15+
            (5, >= 15) => 4,
            (5, >= 12) => 3,
            (5, >= 10) => 2,
            (5, >= 9) => 1,

            // Level 6 - 0 at L1-10, 1 at L11, 2 at L12-14, 3 at L15-16, 4 at L17+
            (6, >= 17) => 4,
            (6, >= 15) => 3,
            (6, >= 12) => 2,
            (6, >= 11) => 1,

            // Level 7 - 0 at L1-12, 1 at L13, 2 at L14-16, 3 at L17-18, 4 at L19+
            (7, >= 19) => 4,
            (7, >= 17) => 3,
            (7, >= 14) => 2,
            (7, >= 13) => 1,

            // Level 8 - 0 at L1-14, 1 at L15, 2 at L16-18, 3 at L19, 4 at L20+
            (8, >= 20) => 4,
            (8, >= 19) => 3,
            (8, >= 16) => 2,
            (8, >= 15) => 1,

            // Level 9 - 0 at L1-16, 1 at L17, 2 at L18, 3 at L19, 4 at L20+
            (9, >= 20) => 4,
            (9, >= 19) => 3,
            (9, >= 18) => 2,
            (9, >= 17) => 1,

            _ => 0
        };
    }

    private int GetClericSpellSlots(int casterLevel, int spellLevel)
    {
        // Cleric spell progression per cls_spgn_cler.2da
        return (spellLevel, casterLevel) switch
        {
            // Level 0 - 3 at L1, 4 at L2-3, 5 at L4-6, 6 at L7+
            (0, >= 7) => 6,
            (0, >= 4) => 5,
            (0, >= 2) => 4,
            (0, >= 1) => 3,

            // Level 1 - 2 at L1, 3 at L2-3, 4 at L4-6, 5 at L7-10, 6 at L11+
            (1, >= 11) => 6,
            (1, >= 7) => 5,
            (1, >= 4) => 4,
            (1, >= 2) => 3,
            (1, >= 1) => 2,

            // Level 2 - 0 at L1-2, 2 at L3, 3 at L4-5, 4 at L6-8, 5 at L9-12, 6 at L13+
            (2, >= 13) => 6,
            (2, >= 9) => 5,
            (2, >= 6) => 4,
            (2, >= 4) => 3,
            (2, >= 3) => 2,

            // Level 3 - 0 at L1-4, 2 at L5, 3 at L6-7, 4 at L8-10, 5 at L11-14, 6 at L15+
            (3, >= 15) => 6,
            (3, >= 11) => 5,
            (3, >= 8) => 4,
            (3, >= 6) => 3,
            (3, >= 5) => 2,

            // Level 4 - 0 at L1-6, 2 at L7, 3 at L8-10, 4 at L11-12, 5 at L13-16, 6 at L17+
            (4, >= 17) => 6,
            (4, >= 13) => 5,
            (4, >= 11) => 4,
            (4, >= 8) => 3,
            (4, >= 7) => 2,

            // Level 5 - 0 at L1-8, 2 at L9, 3 at L10-11, 4 at L12-14, 5 at L15-18, 6 at L19+
            (5, >= 19) => 6,
            (5, >= 15) => 5,
            (5, >= 12) => 4,
            (5, >= 10) => 3,
            (5, >= 9) => 2,

            // Level 6 - 0 at L1-10, 2 at L11, 3 at L12-14, 4 at L15-16, 5 at L17+
            (6, >= 17) => 5,
            (6, >= 15) => 4,
            (6, >= 12) => 3,
            (6, >= 11) => 2,

            // Level 7 - 0 at L1-12, 2 at L13, 3 at L14-16, 4 at L17-18, 5 at L19+
            (7, >= 19) => 5,
            (7, >= 17) => 4,
            (7, >= 14) => 3,
            (7, >= 13) => 2,

            // Level 8 - 0 at L1-14, 2 at L15, 3 at L16-18, 4 at L19-20, 5 at L21+
            (8, >= 21) => 5,
            (8, >= 19) => 4,
            (8, >= 16) => 3,
            (8, >= 15) => 2,

            // Level 9 - 0 at L1-16, 2 at L17, 3 at L18, 4 at L19, 5 at L20+
            (9, >= 20) => 5,
            (9, >= 19) => 4,
            (9, >= 18) => 3,
            (9, >= 17) => 2,

            _ => 0
        };
    }

    private int GetDruidSpellSlots(int casterLevel, int spellLevel)
    {
        // Druid spell progression per cls_spgn_dru.2da
        return (spellLevel, casterLevel) switch
        {
            // Level 0 - 3 at L1, 4 at L2-3, 5 at L4-6, 6 at L7+
            (0, >= 7) => 6,
            (0, >= 4) => 5,
            (0, >= 2) => 4,
            (0, >= 1) => 3,

            // Level 1 - 1 at L1, 2 at L2-3, 3 at L4-6, 4 at L7-10, 5 at L11+
            (1, >= 11) => 5,
            (1, >= 7) => 4,
            (1, >= 4) => 3,
            (1, >= 2) => 2,
            (1, >= 1) => 1,

            // Level 2 - 0 at L1-2, 1 at L3, 2 at L4-5, 3 at L6-8, 4 at L9-12, 5 at L13+
            (2, >= 13) => 5,
            (2, >= 9) => 4,
            (2, >= 6) => 3,
            (2, >= 4) => 2,
            (2, >= 3) => 1,

            // Level 3 - 0 at L1-4, 1 at L5, 2 at L6-7, 3 at L8-10, 4 at L11-14, 5 at L15+
            (3, >= 15) => 5,
            (3, >= 11) => 4,
            (3, >= 8) => 3,
            (3, >= 6) => 2,
            (3, >= 5) => 1,

            // Level 4 - 0 at L1-6, 1 at L7, 2 at L8-10, 3 at L11-12, 4 at L13-16, 5 at L17+
            (4, >= 17) => 5,
            (4, >= 13) => 4,
            (4, >= 11) => 3,
            (4, >= 8) => 2,
            (4, >= 7) => 1,

            // Level 5 - 0 at L1-8, 1 at L9, 2 at L10-11, 3 at L12-14, 4 at L15-18, 5 at L19+
            (5, >= 19) => 5,
            (5, >= 15) => 4,
            (5, >= 12) => 3,
            (5, >= 10) => 2,
            (5, >= 9) => 1,

            // Level 6 - 0 at L1-10, 1 at L11, 2 at L12-14, 3 at L15-16, 4 at L17+
            (6, >= 17) => 4,
            (6, >= 15) => 3,
            (6, >= 12) => 2,
            (6, >= 11) => 1,

            // Level 7 - 0 at L1-12, 1 at L13, 2 at L14-16, 3 at L17-18, 4 at L19+
            (7, >= 19) => 4,
            (7, >= 17) => 3,
            (7, >= 14) => 2,
            (7, >= 13) => 1,

            // Level 8 - 0 at L1-14, 1 at L15, 2 at L16-18, 3 at L19, 4 at L20+
            (8, >= 20) => 4,
            (8, >= 19) => 3,
            (8, >= 16) => 2,
            (8, >= 15) => 1,

            // Level 9 - 0 at L1-16, 1 at L17, 2 at L18, 3 at L19, 4 at L20+
            (9, >= 20) => 4,
            (9, >= 19) => 3,
            (9, >= 18) => 2,
            (9, >= 17) => 1,

            _ => 0
        };
    }

    private int GetPaladinSpellSlots(int casterLevel, int spellLevel)
    {
        // Paladin spell progression per cls_spgn_pal.2da
        // Paladins have no level 0 spells
        return (spellLevel, casterLevel) switch
        {
            // Level 1 - 0 at L4-5, 1 at L6-13, 2 at L14-17, 3 at L18+
            (1, >= 18) => 3,
            (1, >= 14) => 2,
            (1, >= 6) => 1,
            (1, >= 4) => 0,

            // Level 2 - 0 at L8-9, 1 at L10-15, 2 at L16-18, 3 at L19+
            (2, >= 19) => 3,
            (2, >= 16) => 2,
            (2, >= 10) => 1,
            (2, >= 8) => 0,

            // Level 3 - 0 at L11, 1 at L12-16, 2 at L17-18, 3 at L19+
            (3, >= 19) => 3,
            (3, >= 17) => 2,
            (3, >= 12) => 1,
            (3, >= 11) => 0,

            // Level 4 - 0 at L14, 1 at L15-18, 2 at L19, 3 at L20+
            (4, >= 20) => 3,
            (4, >= 19) => 2,
            (4, >= 15) => 1,

            _ => 0
        };
    }

    private int GetRangerSpellSlots(int casterLevel, int spellLevel)
    {
        // Ranger spell progression per cls_spgn_rang.2da
        // Rangers have no level 0 spells (same progression as Paladin)
        return (spellLevel, casterLevel) switch
        {
            // Level 1 - 0 at L4-5, 1 at L6-13, 2 at L14-17, 3 at L18+
            (1, >= 18) => 3,
            (1, >= 14) => 2,
            (1, >= 6) => 1,
            (1, >= 4) => 0,

            // Level 2 - 0 at L8-9, 1 at L10-15, 2 at L16-18, 3 at L19+
            (2, >= 19) => 3,
            (2, >= 16) => 2,
            (2, >= 10) => 1,
            (2, >= 8) => 0,

            // Level 3 - 0 at L11, 1 at L12-16, 2 at L17-18, 3 at L19+
            (3, >= 19) => 3,
            (3, >= 17) => 2,
            (3, >= 12) => 1,
            (3, >= 11) => 0,

            // Level 4 - 0 at L14, 1 at L15-18, 2 at L19, 3 at L20+
            (4, >= 20) => 3,
            (4, >= 19) => 2,
            (4, >= 15) => 1,

            _ => 0
        };
    }
}

