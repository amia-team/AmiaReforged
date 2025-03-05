using AmiaReforged.Classes.EffectUtils;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Dark;

public class DevourMagic
{
    public void CastDevourMagic(uint nwnObjectId)
    {
        uint target = GetSpellTargetObject();
        uint caster = nwnObjectId;
        int warlockLevels = GetLevelByClass(57, caster);
        IntPtr location = GetSpellTargetLocation();

        IntPtr creatureVfx = EffectVisualEffect(VFX_IMP_DESTRUCTION);
        IntPtr objectVfx = EffectVisualEffect(VFX_IMP_BREACH);
        IntPtr aoeVfx = EffectVisualEffect(VFX_FNF_MYSTICAL_EXPLOSION, 0, 0.2f);

        //---------------------------
        // * Casting as single target
        //---------------------------

        SignalEvent(target, EventSpellCastAt(caster, 1014));

        if (NwEffects.GetHasEffectType(EFFECT_TYPE_PETRIFY, target) == TRUE || GetLocalInt(target, "X1_L_IMMUNE_TO_DISPEL") == 10) return;
        if(GetHasSpellEffect(SPELL_TIME_STOP, target) == TRUE)
        {
            ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_GLOBE_USE), target);
            return;
        }
        // Placeables are checked for dispel for every effect
        if(GetObjectType(target) == OBJECT_TYPE_PLACEABLE || GetObjectType(target) == OBJECT_TYPE_DOOR)
        {
            ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDispelMagicAll(warlockLevels), target);
            ApplyEffectToObject(DURATION_TYPE_INSTANT, objectVfx, target);
            return;
        }
        // Creatures other than the caster are checked for dispel for the highest CL effect
        if(GetObjectType(target) == OBJECT_TYPE_CREATURE && GetIsFriend(target, caster) == FALSE)
        {
            int effectCountBefore = NwEffects.GetEffectCount(target);

            ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDispelMagicAll(warlockLevels), target);
            ApplyEffectToObject(DURATION_TYPE_INSTANT, creatureVfx, target);

            // Heals 5 * effect dispelled
            DelayedHeal(NwEffects.RandomFloat(1,2), caster, target, effectCountBefore);
            return;
        }

        //-----------------
        // * Casting as AoE
        //-----------------
        const int objectTypes = OBJECT_TYPE_CREATURE | OBJECT_TYPE_DOOR | OBJECT_TYPE_PLACEABLE | OBJECT_TYPE_AREA_OF_EFFECT;

        uint currentTarget = GetFirstObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_LARGE, location, FALSE, objectTypes);
        SignalEvent(currentTarget, EventSpellCastAt(caster, 1014));

        ApplyEffectAtLocation(DURATION_TYPE_INSTANT, aoeVfx, location);
        // Everything timestopped is immune
        while (GetIsObjectValid(currentTarget) == TRUE)
        {
            // AoE effects and placeables are checked for dispel for every effect
            if (GetObjectType(currentTarget) == OBJECT_TYPE_PLACEABLE || GetObjectType(currentTarget) == OBJECT_TYPE_DOOR)
            {
                ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDispelMagicAll(warlockLevels), currentTarget);
                ApplyEffectToObject(DURATION_TYPE_INSTANT, objectVfx, currentTarget);
                currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_LARGE, location, FALSE, objectTypes);
                continue;
            }
            if (GetObjectType(currentTarget) == OBJECT_TYPE_AREA_OF_EFFECT)
            {
                int aoeCreatorCl = GetCasterLevel(GetAreaOfEffectCreator(currentTarget));
                bool isAura = GetSubString(GetTag(currentTarget), 0, 7) == "VFX_MOB";
                if (!isAura)
                {
                    if (GetAreaOfEffectCreator(currentTarget) == TRUE) DestroyObject(currentTarget);
                    if (NwEffects.DispelCheck(warlockLevels, aoeCreatorCl) == TRUE) DestroyObject (currentTarget);
                }
                currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_LARGE, location, FALSE, objectTypes);
                continue;
            }
            // Creatures other than the caster are checked for dispel for the highest CL effect
            if (NwEffects.IsValidSpellTarget(currentTarget, 2, caster))
            {
                if (NwEffects.GetHasEffectType(EFFECT_TYPE_PETRIFY, target) == TRUE || GetLocalInt(target, "X1_L_IMMUNE_TO_DISPEL") == 10)
                {
                    currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_LARGE, location, FALSE, objectTypes);
                    continue;
                }
                if (GetHasSpellEffect(SPELL_TIME_STOP, currentTarget) == TRUE)
                {
                    ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_GLOBE_USE), currentTarget);
                    currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_LARGE, location, FALSE, objectTypes);
                    continue;
                }

                int effectCountBefore = NwEffects.GetEffectCount(currentTarget);

                ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDispelMagicBest(warlockLevels), currentTarget);
                ApplyEffectToObject(DURATION_TYPE_INSTANT, creatureVfx, currentTarget);

                DelayedHeal(NwEffects.RandomFloat(1,2), caster, currentTarget, effectCountBefore);
            }
            currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_LARGE, location, FALSE, objectTypes);
        }
    }

    private void Heal(uint caster, uint target, int effectCountBefore)
    {
        int effectCountAfter = NwEffects.GetEffectCount(target);
        int effectsDispelled = effectCountBefore - effectCountAfter;
        if (effectsDispelled > 0)
        {
            ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectHeal(effectsDispelled*5), caster);
            ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_HEALING_M), caster);
        }
    }

    private void DelayedHeal(float delay, uint caster, uint target, int effectCountBefore)
    {
        DelayCommand(delay, () => Heal(caster, target, effectCountBefore));
    }
}