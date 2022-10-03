using AmiaReforged.Classes.EffectUtils;
using NWN.Core.NWNX;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Types.EssenceEffects;

public class UtterdarkEssenceEffects : EssenceEffectApplier
{
    public UtterdarkEssenceEffects(uint target, uint caster) : base(target, caster)
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

        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDamage(damage, DAMAGE_TYPE_NEGATIVE), Target);

        if (FortitudeSave(Target, CalculateDc(), SAVING_THROW_TYPE_NEGATIVE) == TRUE) return;
        IntPtr levelDrain = SupernaturalEffect(EffectNegativeLevel(2));
        ApplyEffectToObject(DURATION_TYPE_PERMANENT, levelDrain, Target);
    }
}