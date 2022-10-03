using AmiaReforged.Classes.Types.EssenceEffects;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Types.Shapes;

public static class EldritchSphere
{
    public static void CastEldritchSphere(uint caster, EssenceVisuals essenceVisuals)
    {
        ApplyEffectToObject(DURATION_TYPE_INSTANT, essenceVisuals.DoomVfx, caster);
        uint currentTarget = GetFirstObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_LARGE, GetLocation(caster), TRUE);
        EssenceType essenceType =
            (EssenceType)GetLocalInt(GetItemPossessedBy(caster, "ds_pckey"), "warlock_essence");
        while (GetIsObjectValid(currentTarget) == TRUE)
        {
            EssenceEffectApplier effectApplier =
                EssenceEffectFactory.CreateEssenceEffect(essenceType, currentTarget, caster);
            if (currentTarget == caster || GetIsReactionTypeHostile(currentTarget, caster) == FALSE)
            {
                currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_LARGE, GetLocation(caster), TRUE);
                continue;
            }

            SignalEvent(currentTarget, EventSpellCastAt(caster, 1004));

            bool hasEvasion = GetHasFeat(FEAT_EVASION, currentTarget) == TRUE;
            bool passedSave = ReflexSave(currentTarget, CalculateDc(caster)) == TRUE;

            if (passedSave && hasEvasion)
            {
                currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_LARGE, GetLocation(caster), TRUE);
                continue;
            }

            if (!passedSave && GetHasFeat(FEAT_IMPROVED_EVASION, currentTarget) == TRUE)
            {
                currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_LARGE, GetLocation(caster), TRUE);
                continue;
            }

            int calculatedDamage = GetHasFeat(FEAT_IMPROVED_EVASION) == TRUE
                ? EldritchDamage.CalculateDamageAmount(caster) / 2
                : EldritchDamage.CalculateDamageAmount(caster);
            int finalDamage = passedSave ? calculatedDamage / 2 : calculatedDamage;
            effectApplier.ApplyEffects(finalDamage);
            currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_LARGE, GetLocation(caster), TRUE);
        }
    }

    private static int CalculateDc(uint caster) =>
        GetLevelByClass(57, caster) / 2 + GetAbilityModifier(ABILITY_CHARISMA) + 10;
}