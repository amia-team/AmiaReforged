using AmiaReforged.Classes.Monk;
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
    
    public static int GetSpellDc(SpellEvents.OnSpellCast eventData)
    {
        NwCreature? casterCreature = eventData.Caster as NwCreature;
        NwClass? spellClass = eventData.SpellCastClass?.ClassType;

        if (casterCreature == null || spellClass == null) 
            return eventData.SaveDC;
        
        // Default eventData.SaveDC, exceptions listed before
        return spellClass.ClassType switch
        {
            ClassType.Monk => GetMonkWildMagicDc(casterCreature, eventData.SpellLevel),
            ClassType.Shifter => CalculateShifterDc(casterCreature),
            _ => eventData.SaveDC
        };
    }

    private static int GetMonkWildMagicDc(NwCreature creature, int spellLevel) => 
        MonkUtilFunctions.CalculateMonkDc(creature) - 9 + spellLevel;

    private static int CalculateShifterDc(NwCreature creature) => 
        10 + creature.GetClassInfo(ClassType.Shifter)!.Level / 2 + creature.GetAbilityModifier(Ability.Wisdom);
}