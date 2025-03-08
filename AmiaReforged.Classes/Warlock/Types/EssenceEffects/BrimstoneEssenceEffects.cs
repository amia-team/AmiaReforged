using AmiaReforged.Classes.EffectUtils;
using NWN.Core.NWNX;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Warlock.Types.EssenceEffects;

public class BrimstoneEssenceEffects : EssenceEffectApplier
{
    public BrimstoneEssenceEffects(uint target, uint caster) : base(target, caster)
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

        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDamage(damage, DAMAGE_TYPE_FIRE), Target);
        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_FLAME_S), Target);

        if (NwEffects.GetHasEffectByTag(effectTag: "wlk_brimstone", Target) == TRUE) return;

        bool passedReflexSave = ReflexSave(Target, CalculateDc(), SAVING_THROW_TYPE_FIRE, Caster) == TRUE;

        if (passedReflexSave)
        {
            ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_REFLEX_SAVE_THROW_USE), Target);
            return;
        }

        if (!passedReflexSave)
        {
            int warlockLevels = GetLevelByClass(57, Caster);
            int essenceRounds = warlockLevels / 5;
            float essenceDuration = warlockLevels < 5 ? RoundsToSeconds(1) : RoundsToSeconds(essenceRounds);
            IntPtr burning = TagEffect(EffectVisualEffect(VFX_DUR_INFERNO_CHEST), sNewTag: "is_brimstone");

            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, burning, Target, essenceDuration);
            Burn(essenceRounds);
        }
    }

    private void Burn(int essenceRounds)
    {
        float delay = 6f;
        for (int i = 0; i < essenceRounds; i++)
        {
            DelayCommand(delay,
                () => ApplyEffectToObject(DURATION_TYPE_INSTANT,
                    EffectDamage(d6(2), DAMAGE_TYPE_FIRE), Target));
            delay += 6.0f;
        }
    }
}