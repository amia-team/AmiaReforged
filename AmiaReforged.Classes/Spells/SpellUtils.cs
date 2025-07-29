using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Spells;

public class SpellUtils
{
    /// <summary>
    /// If spell targets neutrals (like fireball, grease, etc., most AOE spells),
    /// it also damages yourself and associates , but not your party members or their associates.
    /// If ObjectTypes in GetObjectsInShape allows placeables or doors, they're always hit
    /// </summary>
    /// <returns>True for valid targets to apply effects to hit</returns>
    static bool IsValidAoeTarget(NwGameObject caster, NwObject target)
    {
        if (target is NwDoor or NwPlaceable) return true;

        // Hurts yourself
        if (caster == target) return true;

        if (target is NwCreature targetCreature)
        {
            // Hurt your own associates
            if (targetCreature.Master == caster) return true;

            // Hurt neutrals and hostiles
            if (!targetCreature.IsReactionTypeFriendly((NwCreature)caster)) return true;
        }

        // Doesn't hurt other targets than the checked for above
        return false;
    }

    /// <summary>
    /// Always use if the spell's effect duration can be extended
    /// </summary>
    /// <param name="metaMagic">The metamagic used on the spellcast; use MetaMagicFeat from the event data</param>
    /// <param name="durationToExtend">Use the effect duration from NwTimespan.FromRounds/Turns/Hours</param>
    /// <returns>The extended duration; if the metamagic wasn't extend, does nothing</returns>
    public static TimeSpan ExtendSpell(MetaMagic metaMagic, TimeSpan durationToExtend) =>
        metaMagic switch
        {
            MetaMagic.Extend => durationToExtend * 2,
            _ => durationToExtend
        };

    /// <summary>
    /// Always use if the spell's value (usually damage) can be empowered
    /// </summary>
    /// <param name="metaMagic">The metamagic used on the spellcast; use MetaMagicFeat from the event data</param>
    /// <param name="valueToEmpower">Typically the damage gotten from Random.Shared.Roll or other calculation</param>
    /// <returns>The empowered value; if empower wasn't used returns the normal value</returns>
    public static int EmpowerSpell(MetaMagic metaMagic, int valueToEmpower)
    {
        return metaMagic switch
        {
            MetaMagic.Empower => (int)(valueToEmpower * 1.5),
            _ => valueToEmpower
        };
    }

    /// <summary>
    /// Always use if the spell's value (usually damage) can be maximized
    /// </summary>
    /// <param name="metaMagic">The metamagic used on the spellcast; use MetaMagicFeat from the event data</param>
    /// <param name="dieSides">The number of sides on the die</param>
    /// <param name="diceAmount">The amount of dice to roll</param>
    /// <returns>The maximized value; if maximize wasn't used returns the normal value</returns>
    public static int MaximizeSpell(MetaMagic metaMagic, int dieSides, int diceAmount)
    {
        return metaMagic switch
        {
            MetaMagic.Maximize => dieSides * diceAmount,
            _ => Random.Shared.Roll(dieSides, diceAmount)
        };
    }

    /// <summary>
    /// A catch-all for all object types so you don't have to always check separately for creature, door, and placeable
    /// </summary>
    public static void SignalSpell(NwGameObject caster, NwGameObject target, NwSpell spell)
    {
        if (target is NwCreature creature) CreatureEvents.OnSpellCastAt.Signal(caster, creature, spell);
        if (target is NwDoor door) DoorEvents.OnSpellCastAt.Signal(caster, door, spell);
        if (target is NwPlaceable placeable) PlaceableEvents.OnSpellCastAt.Signal(caster, placeable, spell);
    }

    /// <summary>
    /// Accounts for custom DCs like Shifter and Monk Wild Magic
    /// </summary>
    /// <returns></returns>
    public static int GetSpellDc(SpellEvents.OnSpellCast eventData)
    {
        NwCreature? casterCreature = eventData.Caster as NwCreature;
        NwClass? spellClass = eventData.SpellCastClass?.ClassType;

        if (casterCreature == null || spellClass == null)
            return eventData.SaveDC;

        // Default eventData.SaveDC, exceptions listed before
        return spellClass.ClassType switch
        {
            ClassType.Monk => CalculateMonkWildMagicDc(casterCreature, eventData.SpellLevel),
            ClassType.Shifter => CalculateShifterDc(casterCreature),
            _ => eventData.SaveDC
        };
    }

    public static int GetAoeSpellDc(AreaOfEffectEvents.OnEnter eventData)
    {
        if (eventData.Effect.Creator is not NwCreature casterCreature) return eventData.SpellSaveDC;
        NwSpell? spell = eventData.Effect.Spell;
        if (spell is null) return eventData.SpellSaveDC;

        // If the caster knows the spell, default to normal DC
        if (casterCreature.Classes.Any(c => c.KnownSpells.Any(spells => spells.Contains(spell))))
            return eventData.SpellSaveDC;

        // Check for both monk and shifter as classes, if caster has both, then use the highest level class
        // or default to shifter if monk and shifter are equal
        if (casterCreature.Classes.All(c => c.Class.ClassType is ClassType.Monk or ClassType.Shifter))
        {
            int monkLevel = casterCreature.GetClassInfo(ClassType.Monk)!.Level;
            int shifterLevel = casterCreature.GetClassInfo(ClassType.Shifter)!.Level;


            return monkLevel > shifterLevel ? CalculateMonkWildMagicDc(casterCreature, spell.InnateSpellLevel)
                : CalculateShifterDc(casterCreature);
        }

        CreatureClassInfo? spellClass = casterCreature.Classes.FirstOrDefault(c => c.Class.ClassType is ClassType.Monk or ClassType.Shifter);
        if (spellClass is null) return eventData.SpellSaveDC;

        return spellClass.Class.ClassType switch
        {
            ClassType.Monk => CalculateMonkWildMagicDc(casterCreature, spell.InnateSpellLevel),
            ClassType.Shifter => CalculateShifterDc(casterCreature),
            _ => eventData.SpellSaveDC
        };
    }

    private static int CalculateMonkWildMagicDc(NwCreature creature, int spellLevel) =>
        0;

    private static int CalculateShifterDc(NwCreature creature)
    {
        int shifterLevel = creature.GetClassInfo(ClassType.Shifter)!.Level;
        int druidLevel = creature.GetClassInfo(ClassType.Druid) != null ? creature.GetClassInfo(ClassType.Druid)!.Level : 0;
        return 10 + (shifterLevel + druidLevel) / 2 + creature.GetAbilityModifier(Ability.Wisdom);
    }

}
