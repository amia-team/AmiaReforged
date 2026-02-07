using AmiaReforged.Classes.Monk.Constants;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Monk.Augmentations.SplinteredChalice;

[ServiceBinding(typeof(OverflowHandler))]
public class OverflowHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public OverflowHandler()
    {
        NwModule.Instance.OnSpellCast += ToggleOverflow;
        Log.Info(message: "Cast Technique Service initialized.");
    }

    private void ToggleOverflow(OnSpellCast eventData)
    {
        if (eventData.Spell?.FeatReference?.Id != MonkFeat.PoeSplinteredChalice
            || !eventData.Caster.IsPlayerControlled(out NwPlayer? player)) return;

        NwCreature? monk = eventData.Caster as NwCreature;
        if (monk == null) return;

        VisualEffectTableEntry? overflowVisual = VfxType.DurProtectionGoodMajor;
        if (overflowVisual == null)
        {
            player.SendServerMessage("Cannot find the vfx for the overflow ability!");
            return;
        }

        Effect? overflow = monk.ActiveEffects.FirstOrDefault(e => e.Tag == Overflow.EffectTag);
        if (overflow != null)
        {
            monk.RemoveEffect(overflow);
            player.GetLoopingVisualEffects(monk)?
                .RemoveAll(vfx => vfx == overflowVisual);

            eventData.PreventSpellCast = true; // This way we don't get the server message "monk embraces pain",
                                               // when they're doing the opposite
            return;
        }

        overflow = GetOverflowEffect(monk);
        player.AddLoopingVisualEffect(monk, overflowVisual);
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

        overflowEffect.SubType = EffectSubType.Extraordinary;
        overflowEffect.Tag = Overflow.EffectTag;

        return overflowEffect;
    }
}
