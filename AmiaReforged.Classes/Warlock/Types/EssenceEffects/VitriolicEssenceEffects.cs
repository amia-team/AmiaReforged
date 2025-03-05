using AmiaReforged.Classes.EffectUtils;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Warlock.Types.EssenceEffects;

public class VitriolicEssenceEffects : EssenceEffectApplier
{
    public VitriolicEssenceEffects(uint target, uint caster) : base(target, caster)
    {
    }

    public override void ApplyEffects(int damage)
    {
        int result = ResistSpell(Caster, Target);
        if (result == 3) return;
        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDamage(damage, DAMAGE_TYPE_ACID), Target);
        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_ACID_S), Target);

        if (NwEffects.GetHasEffectByTag(effectTag: "wlk_vitriolic", Target) == TRUE) return;

        bool passedFortSave = FortitudeSave(Target, CalculateDc(), SAVING_THROW_TYPE_ACID, Caster) == TRUE;


        if (passedFortSave)
        {
            ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_FORTITUDE_SAVING_THROW_USE), Target);
            return;
        }

        if (!passedFortSave)
        {
            int warlockLevels = GetLevelByClass(57, Caster);
            int essenceRounds = warlockLevels / 5;
            float essenceDuration = warlockLevels < 5 ? RoundsToSeconds(1) : RoundsToSeconds(essenceRounds);
            IntPtr burning = TagEffect(EffectVisualEffect(VFX_DUR_AURA_GREEN), sNewTag: "is_vitriolic");

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
                    EffectDamage(d6(2), DAMAGE_TYPE_ACID), Target));
            delay += 6.0f;
        }
    }
}