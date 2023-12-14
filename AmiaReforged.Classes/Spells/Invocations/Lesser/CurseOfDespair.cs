using AmiaReforged.Classes.EffectUtils;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Lesser;

public class CurseOfDespair
{
    public void Run(uint nwnObjectId)
    {
        uint target = GetSpellTargetObject();

        int spellId = GetSpellId();
        SignalEvent(target, EventSpellCastAt(nwnObjectId, spellId));
        IntPtr curvfx = EffectVisualEffect(VFX_IMP_REDUCE_ABILITY_SCORE);
        IntPtr curse = EffectCurse(3, 3, 3, 3, 3, 3);
        IntPtr attack = EffectAttackDecrease(1);

        if (GetIsFriend(target) == TRUE) return;

        if (NwEffects.ResistSpell(nwnObjectId, target)) return;
        if (WillSave(target, GetSpellSaveDC(), SAVING_THROW_WILL, OBJECT_SELF) != TRUE)
        {
            ApplyEffectToObject(DURATION_TYPE_PERMANENT, curse, target);
            ApplyEffectToObject(DURATION_TYPE_INSTANT, curvfx, target);
        }
        else
        {
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, attack, target, 60.0f);
        }
    }
}