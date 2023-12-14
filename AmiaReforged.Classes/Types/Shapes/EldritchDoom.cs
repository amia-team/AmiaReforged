using AmiaReforged.Classes.Types.EssenceEffects;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Types.Shapes;

public static class EldritchDoom
{
    public static void CastEldritchDoom(uint caster, IntPtr location, EssenceVisuals essenceVisuals)
    {
        ApplyEffectAtLocation(DURATION_TYPE_INSTANT, essenceVisuals.DoomVfx, location);
        uint currentTarget = GetFirstObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_HUGE, location, TRUE);
        EssenceType essenceType =
            (EssenceType)GetLocalInt(GetItemPossessedBy(caster, "ds_pckey"), "warlock_essence");
        while (GetIsObjectValid(currentTarget) == TRUE)
        {
            EssenceEffectApplier effectApplier =
                EssenceEffectFactory.CreateEssenceEffect(essenceType, currentTarget, caster);
            if (currentTarget == caster || GetIsFriend(currentTarget, caster) == TRUE || GetIsNeutral(currentTarget, caster) == TRUE)
            {
                currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_HUGE, location, TRUE);
                continue;
            }

            SignalEvent(currentTarget, EventSpellCastAt(caster, 1003));

            bool hasEvasion = GetHasFeat(FEAT_EVASION, currentTarget) == TRUE;
            bool passedSave = ReflexSave(currentTarget, CalculateDc(caster)) == TRUE;

            if (passedSave && hasEvasion)
            {
                currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_HUGE, location, TRUE);
                continue;
            }

            if (!passedSave && GetHasFeat(FEAT_IMPROVED_EVASION, currentTarget) == TRUE)
            {
                currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_HUGE, location, TRUE);
                continue;
            }

            int calculatedDamage = GetHasFeat(FEAT_IMPROVED_EVASION) == TRUE
                ? EldritchDamage.CalculateDamageAmount(caster) / 2
                : EldritchDamage.CalculateDamageAmount(caster);
            int finalDamage = passedSave ? calculatedDamage / 2 : calculatedDamage;
            effectApplier.ApplyEffects(finalDamage);
            currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_LARGE, location, TRUE);
        }
    }

    private static int CalculateDc(uint caster) =>
        GetLevelByClass(57, caster) / 2 + GetAbilityModifier(ABILITY_CHARISMA) + 10;
}