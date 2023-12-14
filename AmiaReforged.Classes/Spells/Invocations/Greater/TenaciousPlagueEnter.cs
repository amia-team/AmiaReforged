using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Greater;

public class TenaciousPlagueEnter
{
    public int Run(uint nwnObjectId)
    {
        uint caster = GetAreaOfEffectCreator(nwnObjectId);

        uint enteringObject = GetEnteringObject();

        bool notValidTarget = enteringObject == caster || GetIsFriend(enteringObject, caster) == TRUE;
        if (notValidTarget) return 0;
        int spellId = GetSpellId();
        SignalEvent(enteringObject, EventSpellCastAt(nwnObjectId, spellId));
        IntPtr bloodEffect = EffectVisualEffect(VFX_COM_BLOOD_REG_RED);

        int casterChaMod = GetAbilityModifier(ABILITY_CHARISMA, caster);
        int damage = casterChaMod < 10 ? d6(2) + casterChaMod : d6(2) + 10;
        
        float delayForEffects = (float)new Random().NextDouble() * (9.0f - 1.0f) + 1.0f;

        ApplyEffectToObject(DURATION_TYPE_PERMANENT, TagEffect(EffectMovementSpeedDecrease(50), "tenacious_plague"),
            enteringObject);
        DelayCommand(delayForEffects, () => ApplyEffectToObject(DURATION_TYPE_INSTANT, bloodEffect, enteringObject));
        DelayCommand(delayForEffects,
            () => ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDamage(damage), enteringObject));

        return 0;
    }
}