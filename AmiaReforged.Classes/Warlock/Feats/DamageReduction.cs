using AmiaReforged.Classes.Warlock.Constants;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Warlock.Feats;

[ServiceBinding(typeof(DamageReduction))]
public class DamageReduction
{
    private const string DamageReductionTag = "wlk_dr";

    public DamageReduction(EventService eventService)
    {
        NwModule.Instance.OnClientEnter += ApplyOnEnter;
        eventService.SubscribeAll<OnLevelUp, OnLevelUp.Factory>(ApplyOnLevelUp, EventCallbackType.After);
    }

    private void ApplyOnEnter(ModuleEvents.OnClientEnter eventData)
    {
        if (eventData.Player.LoginCreature is not { } warlock ||
            !warlock.KnowsFeat(WarlockFeats.DamageReduction!))
            return;

        AdjustDamageReduction(warlock);
    }

    private void ApplyOnLevelUp(OnLevelUp eventData)
    {
        if (!eventData.Creature.KnowsFeat(WarlockFeats.DamageReduction!))
            return;

        AdjustDamageReduction(eventData.Creature);
    }

    private static void AdjustDamageReduction(NwCreature warlock)
    {
        Effect? damageReduction = warlock.ActiveEffects.FirstOrDefault(e => e.Tag == DamageReductionTag);
        if (damageReduction != null)
            warlock.RemoveEffect(damageReduction);

        int warlockLevel = warlock.WarlockLevel();

        DamagePower? damagePower = warlockLevel switch
        {
            >= 3 and < 7 => DamagePower.Plus1,
            >= 7 and < 11 => DamagePower.Plus2,
            >= 11 and < 15 => DamagePower.Plus3,
            >= 15 and < 19 => DamagePower.Plus4,
            >= 19 and < 23 => DamagePower.Plus5,
            >= 23 and < 27 => DamagePower.Plus6,
            >= 27 => DamagePower.Plus7,
            _ => null
        };
        if (damagePower == null) return;

        damageReduction = Effect.DamageReduction(amount: 5, damagePower.Value);
        damageReduction.Tag = DamageReductionTag;
        damageReduction.SubType = EffectSubType.Unyielding;

        warlock.ApplyEffect(EffectDuration.Permanent, damageReduction);
    }
}
