using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Warlock.Types.EssenceEffects;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Warlock.Types.Shapes;

public static class EldritchChain
{
    private static int _damageAmount;
    private static EssenceEffectApplier? _applier;
    private static EssenceType _essenceType;

    public static void CastEldritchChain(uint caster, uint targetObject, EssenceType essence,
        EssenceEffectApplier effectApplier)
    {
        int touchAttackRanged = WarlockConstants.RangedTouch(caster, targetObject);
        if (touchAttackRanged == FALSE) return;

        _applier = effectApplier;
        _essenceType = (EssenceType)GetLocalInt(GetItemPossessedBy(caster, sItemTag: "ds_pckey"),
            sVarName: "warlock_essence");

        SignalEvent(targetObject, EventSpellCastAt(caster, 1005));

        _damageAmount = EldritchDamage.CalculateDamageAmount(caster) * touchAttackRanged;

        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EssenceVfx.Beam(essence, caster), targetObject, 1.1f);
        _applier.ApplyEffects(_damageAmount);

        int chainLimit = GetCasterLevel(caster) / 5;

        uint current = GetFirstObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_COLOSSAL, GetLocation(targetObject), TRUE);
        uint source = current;
        uint chains = 0;

        while (GetIsObjectValid(current) == TRUE && chains < chainLimit)
        {
            if (current == targetObject)
            {
                current = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_COLOSSAL, GetLocation(targetObject), TRUE);
                continue;
            }

            current = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_COLOSSAL, GetLocation(current), TRUE);

            if (NwEffects.IsValidSpellTarget(current, 3, caster))
            {
                ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EssenceVfx.Beam(essence, source), current, 1.1f);

                _damageAmount = EldritchDamage.CalculateDamageAmount(caster) * touchAttackRanged;
                _applier = EssenceEffectFactory.CreateEssenceEffect(_essenceType, current, caster);
                _applier.ApplyEffects(_damageAmount / 2);

                source = current;
                chains++;
            }
        }
    }
}