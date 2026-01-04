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

    // Dictionary mapping prestige classes to their caster level modifier formulas
    private readonly Dictionary<ClassType, Func<int, int>> _prestigeClassModifiers = new()
    {
        { ClassType.DragonDisciple, prcLevel => Math.Max(0, prcLevel) },
        { ClassType.Blackguard, prcLevel => Math.Max(0, prcLevel) }
    };

    // Mapping of prestige classes to their valid base caster classes
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

        Log.Info($"=== FINISHED: ApplyPrestigeSpellSlotSkin for {creature.Name} ===");
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

        for (int spellLevel = 1; spellLevel <= 9; spellLevel++)
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
            ClassType.Assassin => NWScript.CLASS_TYPE_ASSASSIN,
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
            ClassType.Assassin => Ability.Intelligence,
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
        return spellLevel switch
        {
            1 => casterLevel >= 2 ? 4 : 0,
            2 => casterLevel >= 4 ? 3 : 0,
            3 => casterLevel >= 7 ? 3 : 0,
            4 => casterLevel >= 10 ? 3 : 0,
            5 => casterLevel >= 13 ? 2 : 0,
            6 => casterLevel >= 16 ? 2 : 0,
            _ => 0
        };
    }

    private int GetSorcererSpellSlots(int casterLevel, int spellLevel)
    {
        return spellLevel switch
        {
            1 => casterLevel >= 1 ? 5 : 0,
            2 => casterLevel >= 4 ? 5 : 0,
            3 => casterLevel >= 6 ? 5 : 0,
            4 => casterLevel >= 8 ? 5 : 0,
            5 => casterLevel >= 10 ? 4 : 0,
            6 => casterLevel >= 12 ? 4 : 0,
            7 => casterLevel >= 14 ? 4 : 0,
            8 => casterLevel >= 16 ? 4 : 0,
            9 => casterLevel >= 18 ? 3 : 0,
            _ => 0
        };
    }

    private int GetWizardSpellSlots(int casterLevel, int spellLevel)
    {
        return spellLevel switch
        {
            1 => casterLevel >= 1 ? 3 : 0,
            2 => casterLevel >= 3 ? 2 : 0,
            3 => casterLevel >= 5 ? 2 : 0,
            4 => casterLevel >= 7 ? 2 : 0,
            5 => casterLevel >= 9 ? 2 : 0,
            6 => casterLevel >= 11 ? 2 : 0,
            7 => casterLevel >= 13 ? 2 : 0,
            8 => casterLevel >= 15 ? 2 : 0,
            9 => casterLevel >= 17 ? 2 : 0,
            _ => 0
        };
    }

    private int GetClericSpellSlots(int casterLevel, int spellLevel)
    {
        return spellLevel switch
        {
            1 => casterLevel >= 1 ? 3 : 0,
            2 => casterLevel >= 3 ? 2 : 0,
            3 => casterLevel >= 5 ? 2 : 0,
            4 => casterLevel >= 7 ? 2 : 0,
            5 => casterLevel >= 9 ? 2 : 0,
            6 => casterLevel >= 11 ? 2 : 0,
            7 => casterLevel >= 13 ? 2 : 0,
            8 => casterLevel >= 15 ? 2 : 0,
            9 => casterLevel >= 17 ? 2 : 0,
            _ => 0
        };
    }

    private int GetDruidSpellSlots(int casterLevel, int spellLevel)
    {
        return GetClericSpellSlots(casterLevel, spellLevel);
    }

    private int GetPaladinSpellSlots(int casterLevel, int spellLevel)
    {
        return spellLevel switch
        {
            1 => casterLevel >= 4 ? 1 : 0,
            2 => casterLevel >= 8 ? 1 : 0,
            3 => casterLevel >= 12 ? 1 : 0,
            4 => casterLevel >= 16 ? 1 : 0,
            _ => 0
        };
    }

    private int GetRangerSpellSlots(int casterLevel, int spellLevel)
    {
        return spellLevel switch
        {
            1 => casterLevel >= 4 ? 1 : 0,
            2 => casterLevel >= 8 ? 1 : 0,
            3 => casterLevel >= 12 ? 1 : 0,
            4 => casterLevel >= 16 ? 1 : 0,
            _ => 0
        };
    }
}

