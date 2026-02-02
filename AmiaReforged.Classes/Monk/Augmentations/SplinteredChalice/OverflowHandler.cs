using AmiaReforged.Classes.Monk.Constants;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core.NWNX;
using NLog;
using DamageType = Anvil.API.DamageType;
using EffectSubType = Anvil.API.EffectSubType;
using SavingThrow = Anvil.API.SavingThrow;

namespace AmiaReforged.Classes.Monk.Augmentations.SplinteredChalice;

[ServiceBinding(typeof(OverflowHandler))]
public class OverflowHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public OverflowHandler()
    {
        string environment = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE");
        if (environment == "live") return;

        NwModule.Instance.OnSpellCast += ToggleOverflow;
        Log.Info(message: "Cast Technique Service initialized.");
    }

    private void ToggleOverflow(OnSpellCast eventData)
    {
        if (eventData.Spell?.FeatReference?.Id != MonkFeat.PoeSplinteredChalice
            || !eventData.Caster.IsPlayerControlled(out NwPlayer? player)) return;

        NwCreature? monk = eventData.Caster as NwCreature;
        if (monk == null) return;

        Effect? overflow = monk.ActiveEffects.FirstOrDefault(e => e.Tag == OverflowConstant.EffectTag);
        if (overflow != null)
        {
            monk.RemoveEffect(overflow);
            player.GetLoopingVisualEffects(monk)?
                .RemoveAll(vfx => vfx.RowIndex == (int)VfxType.DurProtectionGoodMajor);
            return;
        }

        overflow = GetOverflowEffect(monk);
        monk.ApplyEffect(EffectDuration.Permanent, overflow);
    }

    /// <summary>
    /// The monk may activate their Overflow by deliberately embracing pain. Upon activation,
    /// the monk takes damage equal to twice their hit dice, gains 10% physical damage vulnerability,
    /// and suffers a penalty of -2 to armor class and saving throws.
    /// </summary>
    private static Effect GetOverflowEffect(NwCreature monk)
    {
        int damageAmount = monk.Level * 2;
        monk.ApplyEffect(EffectDuration.Instant, Effect.Damage(damageAmount));

        Effect overflowEffect = Effect.LinkEffects
        (
            Effect.DamageImmunityDecrease(DamageType.Bludgeoning, 10),
            Effect.DamageImmunityDecrease(DamageType.Slashing, 10),
            Effect.DamageImmunityDecrease(DamageType.Slashing, 10),
            Effect.ACDecrease(2),
            Effect.SavingThrowDecrease(SavingThrow.All, 2)
        );

        overflowEffect.ShowIcon = false;

        overflowEffect = Effect.LinkEffects(overflowEffect, Effect.Icon(EffectIcon.Invulnerable!));
        overflowEffect.SubType = EffectSubType.Extraordinary;
        overflowEffect.Tag = OverflowConstant.EffectTag;

        return overflowEffect;
    }
}
