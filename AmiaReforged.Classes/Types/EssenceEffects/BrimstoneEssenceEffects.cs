using AmiaReforged.Classes.EffectUtils;
using NWN.Core.NWNX;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Types.EssenceEffects;

public class BrimstoneEssenceEffects : EssenceEffectApplier
{
    private const string IsBrimstoned = "is_brimstone";

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
            ObjectPlugin.DoSpellLevelAbsorption(Target, Caster);
            return;
        }

        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDamage(damage, DAMAGE_TYPE_FIRE), Target);

        if (ReflexSave(Target, CalculateDc(), SAVING_THROW_TYPE_FIRE) == TRUE) return;

        if (GetLocalInt(Target, IsBrimstoned) == TRUE) return;

        int duration = GetCasterLevel(Caster) / 5;
        float delay = 6.0f;
        ApplyEffectToObject(DURATION_TYPE_TEMPORARY,
            SupernaturalEffect(EffectVisualEffect(VFX_DUR_INFERNO)), Target,
            RoundsToSeconds(duration));

        for (int i = 0; i < duration; i++)
        {
            DelayCommand(delay,
                () => ApplyEffectToObject(DURATION_TYPE_INSTANT,
                    EffectDamage(d6(2), DAMAGE_TYPE_FIRE), Target));
            delay += 6.0f;
        }

        SetLocalInt(Target, IsBrimstoned, TRUE);
        DelayCommand(duration, () => DeleteLocalInt(Target, IsBrimstoned));
    }
}