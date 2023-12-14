using AmiaReforged.Classes.EffectUtils;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Lesser;

public class DreadSeizure
{
    public void Run(uint nwnObjectId)
    {
        uint target = GetSpellTargetObject();

        int spellId = GetSpellId();
        SignalEvent(target, EventSpellCastAt(nwnObjectId, spellId));

        IntPtr seizvfx = EffectVisualEffect(VFX_IMP_CHARM);
        IntPtr slow = EffectMovementSpeedDecrease(30);
        IntPtr attack = EffectLinkEffects(EffectAttackDecrease(3), slow);

        if (GetIsFriend(target) == TRUE) return;

        if (NwEffects.ResistSpell(nwnObjectId, target)) return;
        if (FortitudeSave(target, GetSpellSaveDC(), SAVING_THROW_FORT,
                OBJECT_SELF) == TRUE)
            return;

        AssignCommand(target, () => PlayAnimation(ANIMATION_FIREFORGET_SPASM, 1.5f, 0.5f));
        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, attack, target, 18.0f);
        ApplyEffectToObject(DURATION_TYPE_INSTANT, seizvfx, target);
    }
}