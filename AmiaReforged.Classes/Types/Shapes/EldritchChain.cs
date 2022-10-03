using AmiaReforged.Classes.Types.EssenceEffects;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Types.Shapes;

public static class EldritchChain
{
    private static int _damageAmount;
    private static EssenceEffectApplier _applier;
    private static EssenceVisuals _visuals;
    private static EssenceType _essenceType;

    public static void CastEldritchChain(uint caster, uint targetObject, EssenceVisuals essenceVisuals,
        EssenceEffectApplier effectApplier)
    {
        int touchAttackRanged = TouchAttackRanged(targetObject);
        if (touchAttackRanged == FALSE) return;
        if (GetIsFriend(targetObject, caster) == TRUE)
        {
            SendMessageToPC(caster, "You cannot use this spell on friendly or neutral targets.");
            return;
        }

        _applier = effectApplier;
        _visuals = essenceVisuals;
        _essenceType =
            (EssenceType)GetLocalInt(GetItemPossessedBy(caster, "ds_pckey"), "warlock_essence");

        SignalEvent(targetObject, EventSpellCastAt(caster, 1005));

        _damageAmount = EldritchDamage.CalculateDamageAmount(caster) * touchAttackRanged;

        ApplyEffectToObject(DURATION_TYPE_TEMPORARY,
            EffectBeam(_visuals.BeamVfxConst, caster, BODY_NODE_HAND, FALSE, 2.0f), targetObject, 1.2f);
        _applier.ApplyEffects(_damageAmount);

        int chainLimit = GetCasterLevel(caster) / 5;

        uint current = GetFirstObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_COLOSSAL, GetLocation(targetObject), TRUE);
        uint source = current;
        uint chains = 0;
        while (GetIsObjectValid(current) == TRUE && chains < chainLimit)
        {
            current = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_COLOSSAL, GetLocation(current), TRUE);
            if (current == caster || GetIsFriend(current, caster) == TRUE || GetIsNeutral(current, caster) == TRUE ||
                current == targetObject) continue;


            int newTouch = TouchAttackRanged(current, FALSE);
            if (newTouch == 0) break;

            ApplyEffectToObject(DURATION_TYPE_TEMPORARY,
                EffectBeam(_visuals.BeamVfxConst, source, BODY_NODE_CHEST, FALSE, 2.0f), current, 1.2f);

            _applier = EssenceEffectFactory.CreateEssenceEffect(_essenceType, current, caster);
            _applier.ApplyEffects(EldritchDamage.CalculateDamageAmount(caster) * newTouch / 2);

            source = current;
            chains++;
        }
    }
}