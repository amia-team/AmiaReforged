using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Warlock;
using Anvil.API;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Lesser;

public class DreadSeizureEnter
{
    public void DreadSeizureEnterEffects(NwObject aoe)
    {
        uint caster = GetAreaOfEffectCreator(aoe);
        uint enteringObject = GetEnteringObject();

        // -2 AB and 20% movement decrease on a failed Fort save. 
        IntPtr debuffs = NwEffects.LinkEffectList(new List<IntPtr>
        {
            EffectMovementSpeedDecrease(20),
            EffectAttackDecrease(2)
        });
        debuffs = TagEffect(debuffs, sNewTag: "dreadseizure");

        bool passedFortSave = FortitudeSave(enteringObject, WarlockUtils.CalculateDc(caster), 0, caster) == TRUE;

        // Apply if creature is hostile to the warlock.
        if (NwEffects.IsValidSpellTarget(enteringObject, 3, caster))
        {
            if (enteringObject == caster) return;
            SignalEvent(enteringObject, EventSpellCastAt(caster, 987));
            if (GetHasSpellEffect(987, enteringObject) == TRUE) return;
            if (NwEffects.ResistSpell(caster, enteringObject)) return;
            if (passedFortSave)
                ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_FORTITUDE_SAVING_THROW_USE),
                    enteringObject);
            if (!passedFortSave)
            {
                ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_HEAD_EVIL), enteringObject);
                ApplyEffectToObject(DURATION_TYPE_PERMANENT, debuffs, enteringObject);
            }
        }
    }
}