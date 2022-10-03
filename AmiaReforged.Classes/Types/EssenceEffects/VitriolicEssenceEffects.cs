using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Types.EssenceEffects;

public class VitriolicEssenceEffects : EssenceEffectApplier
{
    private const string IsMelting = "is_melting";

    public VitriolicEssenceEffects(uint target, uint caster) : base(target, caster)
    {
    }

    public override void ApplyEffects(int damage)
    {
        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDamage(damage, DAMAGE_TYPE_ACID), Target);

        if (GetLocalInt(Target, IsMelting) == TRUE) return;

        int duration = GetCasterLevel(Caster) / 5;
        float delay = 6.0f;

        for (int i = 0; i < duration; i++)
        {
            DelayCommand(delay,
                () => ApplyEffectToObject(DURATION_TYPE_INSTANT,
                    EffectDamage(d6(2), DAMAGE_TYPE_ACID), Target));
            delay += 6.0f;
        }

        SetLocalInt(Target, IsMelting, TRUE);
        DelayCommand(duration, () => DeleteLocalInt(Target, IsMelting));
    }
}