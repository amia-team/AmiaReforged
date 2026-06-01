using Anvil.API;

namespace AmiaReforged.Classes.EffectUtils.Polymorph;

public static class PolymorphUtils
{
    public static bool PreventDoublePolymorph(NwCreature caster)
    {
        if (caster.ActiveEffects.All(e => e.EffectType != EffectType.Polymorph)) return false;
        caster.ControllingPlayer?.SendServerMessage("Cannot polymorph while polymorphed. Unshift first.");
        return true;
    }
}
