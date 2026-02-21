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
        { ClassType.Blackguard, prcLevel => Math.Max(0, prcLevel) }
    };

    // Mapping of prestige classes to their valid base caster classes
    // NOTE: Only Blackguard needs custom spell slot handling due to classes.2da limitations
    // Other prestige classes (Pale Master, Arcane Archer, Dragon Disciple) are handled correctly by Classes.2da
    private static readonly Dictionary<ClassType, HashSet<ClassType>> PrestigeToBaseCasterMap = new()
    {
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
        NwItem? existingHide = creature.GetItemInSlot(InventorySlot.CreatureSkin);

        if (existingHide != null && existingHide.ResRef == "ds_pchide")
        {
            Log.Info($"STEP 1: ✓ Found equipped PCHIDE: Name='{existingHide.Name}', ResRef='{existingHide.ResRef}' - will reuse");
        }
        else if (existingHide != null)
        {
            Log.Info($"STEP 1: Found non-PCHIDE item in skin slot: '{existingHide.Name}' - cannot modify it");
            existingHide = null;
        }
        else
        {
            Log.Info($"STEP 1: ✗ No creature hide found in skin slot");
        }

        // STEP 2: Clean up any duplicate hides in inventory (but don't touch the equipped one)
        List<NwItem> hideInInventory = creature.Inventory.Items
            .Where(item => item.ResRef == "ds_pchide")
            .ToList();

        if (hideInInventory.Count > 0)
        {
            foreach (NwItem hide in hideInInventory)
            {
                hide.Destroy();
            }
            Log.Info($"STEP 2: ✓ Inventory duplicates removed");
        }
        else
        {
            Log.Info($"STEP 2: No duplicate hides in inventory");
        }

        // STEP 3: Analyze character classes BEFORE creating hide
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

        // Check if we need to add spell slots - ONLY if they have Blackguard
        if (prestigeClasses.Count == 0)
        {

            // If they have a hide but no longer need it, remove it
            if (existingHide != null)
            {
                Log.Info($"STEP 3: Character no longer has prestige classes - removing hide");
                existingHide.Destroy();
            }

            Log.Info($"=== FINISHED: No hide needed for {creature.Name} ===");
            return;
        }
        else if (allBaseClasses.Count == 0)
        {
            Log.Info($"STEP 3: Has prestige class but no valid base caster classes");

            // If they have a hide but no valid base classes, remove it
            if (existingHide != null)
            {
                Log.Info($"STEP 3: Character has no valid base caster classes - removing hide");
                existingHide.Destroy();
            }

            Log.Info($"=== FINISHED: No hide needed for {creature.Name} ===");
            return;
        }
        else
        {
            Log.Info($"STEP 3: ✓ Found {prestigeClasses.Count} prestige caster class(es) and {allBaseClasses.Count} base caster class(es)");
        }

        // STEP 3b: Get or create creature hide
        NwItem? creatureHide = existingHide;

        if (creatureHide == null)
        {
            Log.Info($"STEP 3b: Creating new creature hide...");
            creatureHide = await NwItem.Create("ds_pchide", creature);

            if (creatureHide == null)
            {
                Log.Error($"STEP 3b: ✗✗✗ FAILED to create creature hide!");
                return;
            }

            Log.Info($"STEP 3b: ✓ Creature hide created successfully - Name='{creatureHide.Name}', ResRef='{creatureHide.ResRef}'");
            creatureHide.Droppable = false;
            Log.Info($"STEP 3b: ✓ Set hide as non-droppable");
        }
        else
        {
            Log.Info($"STEP 3b: ✓ Reusing existing hide - will update properties");
        }

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

        // STEP 5: Equip the creature hide if it's not already equipped
        NwItem? equippedHide = creature.GetItemInSlot(InventorySlot.CreatureSkin);

        if (equippedHide == null || equippedHide != creatureHide)
        {
            Log.Info($"STEP 5: Equipping creature hide to skin slot...");
            await NwTask.NextFrame();

            // Use AssignCommand to equip, which may bypass some item level restrictions
            NWScript.AssignCommand(creature, () =>
            {
                NWScript.ActionEquipItem(creatureHide, NWScript.INVENTORY_SLOT_CARMOUR);
            });

            await NwTask.NextFrame();
            equippedHide = creature.GetItemInSlot(InventorySlot.CreatureSkin);
        }
        else
        {
            Log.Info($"STEP 5: Hide already equipped - no need to re-equip");
        }

        if (equippedHide != null && equippedHide == creatureHide)
        {
            Log.Info($"STEP 5: ✓✓✓ SUCCESS - Creature hide equipped and verified!");
            Log.Info($"STEP 5: Equipped hide details - Name='{equippedHide.Name}', ResRef='{equippedHide.ResRef}', Property Count={equippedHide.ItemProperties.Count()}");
        }
        else if (equippedHide != null)
        {
            Log.Error($"STEP 5: ✗ WRONG ITEM EQUIPPED - Expected our hide but found: Name='{equippedHide.Name}', ResRef='{equippedHide.ResRef}'");

            // Item level restriction likely prevented equipping - destroy the hide and notify player
            creatureHide.Destroy();
            if (creature.IsPlayerControlled(out NwPlayer? player))
            {
                player.SendServerMessage("Your prestige spell slots will be available when you reach a higher character level.", ColorConstants.Orange);
            }
            Log.Warn($"STEP 5: Hide could not be equipped (likely item level restriction). It has been removed.");
            return;
        }
        else
        {
            Log.Error($"STEP 5: ✗✗✗ FAILED - Nothing equipped in skin slot!");

            // Item level restriction likely prevented equipping - destroy the hide and notify player
            creatureHide.Destroy();
            if (creature.IsPlayerControlled(out NwPlayer? player))
            {
                player.SendServerMessage("Your prestige spell slots will be available when you reach a higher character level.", ColorConstants.Orange);
            }
            Log.Warn($"STEP 5: Hide could not be equipped (likely item level restriction). It has been removed.");
            return;
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

        // Remove all existing bonus spell slot properties for this class from the item
        Log.Info($"  Removing existing spell slot properties for {baseClass}...");
        int propertiesRemoved = 0;

        foreach (ItemProperty prop in item.ItemProperties.ToList())
        {
            if (prop.Property.PropertyType == ItemPropertyType.BonusSpellSlotOfLevelN)
            {
                // Check if this property is for our class by checking the subtype
                // SubType for BonusSpellSlotOfLevelN is the class constant
                if (prop.SubType?.RowIndex == classConst)
                {
                    item.RemoveItemProperty(prop);
                    propertiesRemoved++;
                }
            }
        }

        Log.Info($"  Removed {propertiesRemoved} existing spell slot properties for {baseClass}");

        int totalPropertiesAdded = 0;

        for (int spellLevel = 0; spellLevel <= 9; spellLevel++)
        {
            // Calculate slots at actual level vs effective level
            int actualBaseSlots = GetBaseSpellSlotsForLevel(baseClass, actualLevel, spellLevel);
            // Only count ability bonus if character has access to this spell level (at least 1 base slot)
            int actualBonusSlots = actualBaseSlots > 0 ? GetBonusSpellSlots(abilityModifier, spellLevel) : 0;
            int actualTotal = actualBaseSlots + actualBonusSlots;

            int effectiveBaseSlots = GetBaseSpellSlotsForLevel(baseClass, effectiveLevel, spellLevel);
            // Only count ability bonus if character has access to this spell level (at least 1 base slot)
            int effectiveBonusSlots = effectiveBaseSlots > 0 ? GetBonusSpellSlots(abilityModifier, spellLevel) : 0;
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
            ClassType.Cleric => NWScript.IP_CONST_CLASS_CLERIC,
            ClassType.Druid => NWScript.IP_CONST_CLASS_DRUID,
            ClassType.Ranger => NWScript.IP_CONST_CLASS_RANGER,
            _ => (int)classType
        };
    }

    private int GetAbilityModifier(NwCreature creature, ClassType classType)
    {
        Ability ability = classType switch
        {
            ClassType.Cleric => Ability.Wisdom,
            ClassType.Druid => Ability.Wisdom,
            ClassType.Ranger => Ability.Wisdom,
            _ => throw new ArgumentException($"Unsupported class type: {classType}")
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
            ClassType.Cleric => GetClericSpellSlots(casterLevel, spellLevel),
            ClassType.Druid => GetDruidSpellSlots(casterLevel, spellLevel),
            ClassType.Ranger => GetRangerSpellSlots(casterLevel, spellLevel),
            _ => 0
        };
    }

    // NWN spell slot progression tables
    private int GetClericSpellSlots(int casterLevel, int spellLevel)
    {
        // Cleric spell progression per cls_spgn_cler.2da
        return (spellLevel, casterLevel) switch
        {
            // Level 1 spells
            (1, >= 11) => 6,
            (1, >= 7) => 5,
            (1, >= 4) => 4,
            (1, >= 2) => 3,
            (1, >= 1) => 2,

            // Level 2 spells
            (2, >= 13) => 6,
            (2, >= 9) => 5,
            (2, >= 6) => 4,
            (2, >= 4) => 3,
            (2, >= 3) => 2,

            // Level 3 spells
            (3, >= 15) => 6,
            (3, >= 11) => 5,
            (3, >= 8) => 4,
            (3, >= 6) => 3,
            (3, >= 5) => 2,

            // Level 4 spells
            (4, >= 17) => 6,
            (4, >= 13) => 5,
            (4, >= 10) => 4,
            (4, >= 8) => 3,
            (4, >= 7) => 2,

            // Level 5 spells
            (5, >= 19) => 6,
            (5, >= 15) => 5,
            (5, >= 12) => 4,
            (5, >= 10) => 3,
            (5, >= 9) => 2,

            // Level 6 spells
            (6, >= 17) => 5,
            (6, >= 14) => 4,
            (6, >= 12) => 3,
            (6, >= 11) => 2,

            // Level 7 spells
            (7, >= 19) => 5,
            (7, >= 16) => 4,
            (7, >= 14) => 3,
            (7, >= 13) => 2,

            // Level 8 spells
            (8, >= 20) => 5,
            (8, >= 18) => 4,
            (8, >= 16) => 3,
            (8, >= 15) => 2,

            // Level 9 spells
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
            // Level 1 spells
            (1, >= 11) => 5,
            (1, >= 7) => 4,
            (1, >= 4) => 3,
            (1, >= 2) => 2,
            (1, >= 1) => 1,

            // Level 2 spells
            (2, >= 13) => 5,
            (2, >= 9) => 4,
            (2, >= 6) => 3,
            (2, >= 4) => 2,
            (2, >= 3) => 1,

            // Level 3 spells
            (3, >= 15) => 5,
            (3, >= 11) => 4,
            (3, >= 8) => 3,
            (3, >= 6) => 2,
            (3, >= 5) => 1,

            // Level 4 spells
            (4, >= 17) => 5,
            (4, >= 13) => 4,
            (4, >= 10) => 3,
            (4, >= 8) => 2,
            (4, >= 7) => 1,

            // Level 5 spells
            (5, >= 19) => 5,
            (5, >= 15) => 4,
            (5, >= 12) => 3,
            (5, >= 10) => 2,
            (5, >= 9) => 1,

            // Level 6 spells
            (6, >= 17) => 4,
            (6, >= 14) => 3,
            (6, >= 12) => 2,
            (6, >= 11) => 1,

            // Level 7 spells
            (7, >= 19) => 4,
            (7, >= 16) => 3,
            (7, >= 14) => 2,
            (7, >= 13) => 1,

            // Level 8 spells
            (8, >= 20) => 4,
            (8, >= 18) => 3,
            (8, >= 16) => 2,
            (8, >= 15) => 1,

            // Level 9 spells
            (9, >= 20) => 4,
            (9, >= 19) => 3,
            (9, >= 18) => 2,
            (9, >= 17) => 1,


            _ => 0
        };
    }

    private int GetRangerSpellSlots(int casterLevel, int spellLevel)
    {
        // Ranger spell progression per cls_spgn_rang.2da
        // Rangers have no level 0 spells
        return (spellLevel, casterLevel) switch
        {
            // Level 1 spells
            (1, >= 18) => 3,
            (1, >= 15) => 2,
            (1, >= 6) => 1,

            // Level 2 spells
            (2, >= 19) => 3,
            (2, >= 16) => 2,
            (2, >= 10) => 1,

            // Level 3 spells
            (3, >= 19) => 3,
            (3, >= 17) => 2,
            (3, >= 12) => 1,

            // Level 4 spells
            (4, >= 20) => 3,
            (4, >= 19) => 2,
            (4, >= 15) => 1,

            _ => 0
        };
    }
}

