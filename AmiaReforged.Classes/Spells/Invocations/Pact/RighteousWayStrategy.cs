using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Spells.Invocations.Pact.Types.Contracts;
using Anvil.API;
using NWN.Core;

namespace AmiaReforged.Classes.Spells.Invocations.Pact;

public sealed class RighteousWayStrategy : IMobilityStrategy
{
    public void Move(NwCreature caster, Location location)
    {
        string cooldown = $"wlkcooldown{caster.LoginPlayer.PlayerName}";
        if (NWScript.GetLocalInt(NwModule.Instance, cooldown) == NWScript.TRUE)
        {
            caster.LoginPlayer.SendServerMessage("-- This effect is still on cooldown --");
            return;
        }

        float duration = 300.0f;
        NWScript.ApplyEffectToObject(NWScript.DURATION_TYPE_TEMPORARY, NWScript.EffectVisualEffect(689), caster, duration / 2);
        NWScript.ApplyEffectToObject(NWScript.DURATION_TYPE_TEMPORARY, NWScript.EffectDisappearAppear(location), caster, 3.0f);
        NWScript.ApplyEffectToObject(NWScript.DURATION_TYPE_TEMPORARY, NWScript.EffectVisualEffect(NWScript.VFX_DUR_GLOW_WHITE), caster, duration);
        NWScript.ApplyEffectToObject(NWScript.DURATION_TYPE_TEMPORARY, NwEffects.LinkEffectList(new List<IntPtr>
        {
            NWScript.EffectSkillDecrease(NWScript.SKILL_HIDE, 100)
        }), caster, duration);
        NWScript.SetLocalInt(NwModule.Instance, cooldown, NWScript.TRUE);
        NWScript.DelayCommand(duration, () => NWScript.DeleteLocalInt(NwModule.Instance, cooldown));
        NWScript.DelayCommand(duration, () => caster.LoginPlayer.SendServerMessage("-- The Luminous Way is now open to you.--"));
    }
}