using NWN.Core;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.EffectUtils;

public static class NwEffects
{
    /// <summary>
    ///     Links all effects in a provided list.
    /// </summary>
    /// <param name="effects">The list of effects to link together</param>
    /// <returns>A single effect that has linked all given effects</returns>
    public static IntPtr LinkEffectList(IReadOnlyList<IntPtr> effects)
    {
        IntPtr linkedEffects = EffectLinkEffects(effects[0], effects[1]);

        return effects.Where(effect => effect != effects[0] && effect != effects[1])
            .Aggregate(linkedEffects, EffectLinkEffects);
    }

    /// <summary>
    ///     Removes area of effects of a given tag belonging to a specific caster by emitting a sphere with a colossal radius
    ///     and searching for AoE effects within that shape.
    /// </summary>
    /// <param name="location">Where the sphere will be emitted from</param>
    /// <param name="caster">The person who created the AoE being searched for</param>
    /// <param name="aoeTag">The tag of the AoE in question, sourced from vfx_persistent.2da</param>
    /// <param name="radiusSize"></param>
    public static void RemoveAoeWithTag(IntPtr location, uint caster, string aoeTag, float radiusSize)
    {
        uint currentObject =
            GetFirstObjectInShape(SHAPE_SPHERE, radiusSize, location,
                FALSE, OBJECT_TYPE_AREA_OF_EFFECT);

        while (GetIsObjectValid(currentObject) == TRUE)
        {
            if (GetAreaOfEffectCreator(currentObject) != caster)
            {
                currentObject =
                    GetNextObjectInShape(SHAPE_SPHERE, radiusSize, location,
                        FALSE,
                        OBJECT_TYPE_AREA_OF_EFFECT);
                continue;
            }

            if (GetTag(currentObject) == aoeTag) DestroyObject(currentObject);

            currentObject = GetNextObjectInShape(SHAPE_SPHERE, radiusSize,
                location, FALSE,
                OBJECT_TYPE_AREA_OF_EFFECT);
        }
    }

    /// <summary>
    ///     Determines whether or not a creature is polymorphed.
    /// </summary>
    /// <param name="nwnObjectId">Object ID of the creature under scrutiny.</param>
    /// <returns></returns>
    public static bool IsPolymorphed(uint nwnObjectId)
    {
        IntPtr effect = GetFirstEffect(nwnObjectId);

        bool poly = false;
        while (GetIsEffectValid(effect) == TRUE)
        {
            if (GetEffectType(effect) == EFFECT_TYPE_POLYMORPH)
            {
                poly = true;
                break;
            }

            effect = GetNextEffect(nwnObjectId);
        }

        return poly;
    }

    /// <summary>
    ///     C# implementation of MyResistSpell that is infinitely less convoluted.
    /// </summary>
    /// <param name="caster">The creature that cast the spell.</param>
    /// <param name="target">The creature that is the target of the spell.</param>
    /// <returns></returns>
    public static bool ResistSpell(uint caster, uint target)
    {
        int resistSpell = NWScript.ResistSpell(caster, target);

        switch (resistSpell)
        {
            case 1 or 2:
                ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_DUR_MAGIC_RESISTANCE), target);
                break;
            case 3:
                ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_SPELL_MANTLE_USE), target);
                break;
        }

        return resistSpell > 0;
    }

    public static bool HasMantle(uint target)
    {
        return GetHasSpellEffect(SPELL_LESSER_SPELL_MANTLE, target) == TRUE ||
               GetHasSpellEffect(SPELL_SPELL_MANTLE, target) == TRUE ||
               GetHasSpellEffect(SPELL_GREATER_SPELL_MANTLE, target) == TRUE;
    }
}