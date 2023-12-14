using AmiaReforged.Classes.EffectUtils;
using NWN.Core.NWNX;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Types.EssenceEffects;

public class BeshadowedEssenceEffects : EssenceEffectApplier
{
    private const int OneRound = 1;

    public BeshadowedEssenceEffects(uint target, uint caster) : base(target, caster)
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

        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDamage(damage), Target);

        bool targetMakesSave = FortitudeSave(Target, CalculateDc()) == TRUE;
        if (targetMakesSave) return;

        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectBlindness(), Target,
            RoundsToSeconds(OneRound));
    }
}