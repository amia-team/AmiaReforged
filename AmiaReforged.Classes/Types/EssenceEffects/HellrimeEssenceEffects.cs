using AmiaReforged.Classes.EffectUtils;
using NWN.Core.NWNX;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Types.EssenceEffects;

public class HellrimeEssenceEffects : EssenceEffectApplier
{
    public HellrimeEssenceEffects(uint target, uint caster) : base(target, caster)
    {
    }

    public override void ApplyEffects(int damage)
    {
        bool resistSpell = NwEffects.ResistSpell(Caster, Target);
        if (resistSpell) return;
        if (NwEffects.HasMantle(Target) && !resistSpell)
        {
            ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_SPELL_MANTLE_USE), Target);
            ObjectPlugin.DoSpellLevelAbsorption(Target, Caster, -1, 9, 0);
            return;
        }

        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDamage(damage, DAMAGE_TYPE_COLD), Target);
        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_FROST_L, 0, 0.5f), Target);

        bool passedFortSave = FortitudeSave(Target, CalculateDC(), SAVING_THROW_TYPE_COLD, Caster) == TRUE;

        if (passedFortSave)
        {
            ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_FORTITUDE_SAVING_THROW_USE), Target);
            return;
        }
        if (!passedFortSave)
        {
            int warlockLevels = GetLevelByClass(57, Caster);
            float essenceDuration = warlockLevels < 5 ? RoundsToSeconds(1) : RoundsToSeconds(warlockLevels / 5);
            IntPtr essenceEffect = NwEffects.LinkEffectList(new List<IntPtr>
            {
                EffectVisualEffect(VFX_DUR_ICESKIN),
                EffectAbilityDecrease(ABILITY_DEXTERITY, 4)
            });
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, essenceEffect, Target, essenceDuration);
        }
    }
}