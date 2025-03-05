using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Types;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Lesser;

public class CurseOfDespair
{
    public void CastCurseOfDespair(uint nwnObjectId)
    {
        uint target = GetSpellTargetObject();
        uint caster = nwnObjectId;
        int warlockLevels = GetLevelByClass(57, nwnObjectId);
        float duration = RoundsToSeconds(warlockLevels);
        IntPtr curse = SupernaturalEffect(EffectCurse(3, 3, 3, 3, 3, 3));
        IntPtr attackDecrease = SupernaturalEffect(EffectAttackDecrease(1));
        attackDecrease = TagEffect(attackDecrease, "curse_wlk_ab_decrease");
        IntPtr location = GetLocation(target);

        ApplyEffectAtLocation(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_PULSE_NEGATIVE), location);
        uint currentTarget = GetFirstObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_LARGE, location, FALSE, OBJECT_TYPE_CREATURE);
        while (GetIsObjectValid(currentTarget) == TRUE)
        {
            if(NwEffects.IsValidSpellTarget(currentTarget, 3, caster))
            {
                SignalEvent(currentTarget, EventSpellCastAt(nwnObjectId, 1000));

                if (NwEffects.ResistSpell(nwnObjectId, currentTarget)){
                    currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_LARGE, location, FALSE, OBJECT_TYPE_CREATURE);
                    continue;
                }

                bool passedWillSave = WillSave(currentTarget, WarlockConstants.CalculateDC(caster), 0, caster) == TRUE;

                if (passedWillSave || NwEffects.GetHasEffectType(EFFECT_TYPE_CURSE, currentTarget) == TRUE ||
                GetIsImmune(currentTarget, IMMUNITY_TYPE_ABILITY_DECREASE | IMMUNITY_TYPE_CURSED) == TRUE)
                {
                    NwEffects.RemoveEffectByTag("curse_wlk_ab_decrease", currentTarget);
                    ApplyEffectToObject(DURATION_TYPE_TEMPORARY, attackDecrease, currentTarget, 60f);
                    ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_CESSATE_NEGATIVE), currentTarget, 60f);
                    ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_HEAD_EVIL), currentTarget);
                    currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_LARGE, location, FALSE, OBJECT_TYPE_CREATURE);
                    continue;
                }
                if (!passedWillSave){
                    ApplyEffectToObject(DURATION_TYPE_TEMPORARY, curse, currentTarget, duration);
                    ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_CESSATE_NEGATIVE), currentTarget, duration);
                    ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_REDUCE_ABILITY_SCORE), currentTarget);
                }
            }

            currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_LARGE, location, FALSE, OBJECT_TYPE_CREATURE);
        }
    }
}