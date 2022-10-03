using AmiaReforged.Classes.Spells.Invocations.Pact.Types.Contracts;
using Anvil.API;
using NWN.Core;

namespace AmiaReforged.Classes.Spells.Invocations.Pact;

public sealed class TrespassStrategy : IMobilityStrategy
{
    private const float Duration = 300.0f;

    public void Move(NwCreature caster, Location location)
    {
        string cooldown = $"wlkcooldown{caster.LoginPlayer.PlayerName}";
        if (NWScript.GetLocalInt(NwModule.Instance, cooldown) == NWScript.TRUE)
        {
            caster.LoginPlayer.SendServerMessage("-- This effect is still on cooldown --");
            return;
        }

        Location locationWithCasterRotation = Location.Create(location.Area, location.Position, caster.Rotation);

        NWScript.ApplyEffectAtLocation(NWScript.DURATION_TYPE_INSTANT,
            NWScript.EffectVisualEffect(NWScript.VFX_IMP_DEATH_WARD), locationWithCasterRotation);
        NWScript.ApplyEffectAtLocation(NWScript.DURATION_TYPE_INSTANT,
            NWScript.EffectVisualEffect(NWScript.VFX_FNF_LOS_EVIL_10), locationWithCasterRotation);
        NWScript.ApplyEffectAtLocation(NWScript.DURATION_TYPE_INSTANT, NWScript.EffectVisualEffect(471),
            locationWithCasterRotation);

        NWScript.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, NWScript.EffectVisualEffect(472), caster);
 
        // NWScript.ApplyEffectToObject(NWScript.DURATION_TYPE_TEMPORARY, RandomNegativeEffect(caster), caster, Duration);

        NWScript.SetLocalInt(NwModule.Instance, cooldown, NWScript.TRUE);
        NWScript.DelayCommand(Duration, () => NWScript.DeleteLocalInt(NwModule.Instance, cooldown));
        NWScript.DelayCommand(Duration,
            () => caster.LoginPlayer.SendServerMessage("-- You may trespass the space between spaces yet again... --"));
    }

    // private static IntPtr RandomNegativeEffect(NwCreature caster)
    // {
    //     if (NWScript.d100() >= 10) return NWScript.EffectVisualEffect(NWScript.VFX_DUR_GLOW_GREY);
    //
    //     int random = new Random().Next(1, 4);
    //     IntPtr randomEffect = random switch
    //     {
    //         1 => DistantConsciousness(caster),
    //         2 => FearTheDark(caster),
    //         3 => PlanarDysphoria(caster),
    //         4 => UnwantedPassenger(caster),
    //         _ => NWScript.EffectVisualEffect(NWScript.VFX_DUR_GLOW_GREY)
    //     };
    //
    //     return randomEffect;
    // }
    //
    // private static IntPtr DistantConsciousness(NwCreature caster)
    // {
    //     IntPtr randomEffect = NwEffects.LinkEffectList(new List<IntPtr>
    //     {
    //         NWScript.EffectSavingThrowDecrease(NWScript.SAVING_THROW_WILL, -4),
    //         NWScript.EffectVisualEffect(NWScript.VFX_DUR_MIND_AFFECTING_DOMINATED)
    //     });
    //     caster.LoginPlayer.SendServerMessage(
    //         "Seconds felt like hours, and now here you stand. Your near-absent mind is malleable and soft, more easily molded by those with the power to shape it...");
    //     return randomEffect;
    // }
    //
    // private static IntPtr FearTheDark(NwCreature caster)
    // {
    //     IntPtr randomEffect = NwEffects.LinkEffectList(new List<IntPtr>
    //     {
    //         NWScript.EffectSkillDecrease(NWScript.SKILL_HIDE, 50),
    //         NWScript.EffectSavingThrowDecrease(NWScript.SAVING_THROW_WILL, 10, NWScript.SAVING_THROW_TYPE_FEAR),
    //         NWScript.EffectVisualEffect(NWScript.VFX_DUR_PDK_FEAR)
    //     });
    //     caster.LoginPlayer.SendServerMessage(
    //         "You fell through a cloud of unassailable darkness, reaching out for something...anything...to return to your home plane. For a short time, you possess a crippling fear of the shadows, and feel as though something hunts you...");
    //     caster.SpeakString("*babbles incoherently.*");
    //     string afraidOfTheDark = $"{caster.Name}{caster.LoginPlayer.PlayerName}afraidOfTheDark";
    //     NWScript.SetLocalInt(NwModule.Instance, afraidOfTheDark, NWScript.TRUE);
    //     NWScript.DelayCommand(Duration, () => NWScript.DeleteLocalInt(NwModule.Instance, afraidOfTheDark));
    //     NWScript.DelayCommand(Duration,
    //         () => caster.LoginPlayer.SendServerMessage("You begin to calm down and no longer feel hunted."));
    //     return randomEffect;
    // }
    //
    // private static IntPtr PlanarDysphoria(NwCreature caster)
    // {
    //     IntPtr randomEffect = NwEffects.LinkEffectList(new List<IntPtr>
    //     {
    //         NWScript.EffectACDecrease(3),
    //         NWScript.EffectVisualEffect(NWScript.VFX_DUR_BLUR)
    //     });
    //     randomEffect = NWScript.SupernaturalEffect(randomEffect);
    //     caster.LoginPlayer.SendServerMessage(
    //         "A dysphoric sensation washes over you. You do not belong here. This is not your body. You could not care less what happens to it.");
    //     NWScript.DelayCommand(Duration,
    //         () => caster.LoginPlayer.SendServerMessage(
    //             "You find yourself. You remember that this vessel is truly yours, for better or for worse."));
    //     return randomEffect;
    // }
    //
    // private static IntPtr UnwantedPassenger(NwCreature caster)
    // {
    //     IntPtr randomEffect = NwEffects.LinkEffectList(new List<IntPtr>
    //     {
    //         NWScript.EffectMovementSpeedDecrease(60),
    //         NWScript.EffectVisualEffect(NWScript.VFX_DUR_AURA_RED_DARK)
    //     });
    //     caster.LoginPlayer.SendServerMessage(
    //         "Something heavy hangs from your shoulders. You feel as if a pair of cold, putrid arms are wrapped around your shoulders.");
    //     NWScript.DelayCommand(Duration,
    //         () => caster.LoginPlayer.SendServerMessage(
    //             "You feel normal again."));
    //     return randomEffect;
    // }
}