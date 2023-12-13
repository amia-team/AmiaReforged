using NWN.Core;

namespace AmiaReforged.Classes.Types.EssenceEffects;

public abstract class EssenceEffectApplier
{
    protected readonly uint Caster;
    protected readonly uint Target;

    protected EssenceEffectApplier(uint target, uint caster)
    {
        Target = target;
        Caster = caster;
    }

    public abstract void ApplyEffects(int damage);

    protected int CalculateDC()
    {
        return (NWScript.GetLevelByClass(57, Caster) / 3) + NWScript.GetAbilityModifier(NWScript.ABILITY_CHARISMA, Caster) + 10;
    }
}