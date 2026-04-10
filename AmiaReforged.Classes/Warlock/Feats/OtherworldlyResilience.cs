using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Warlock.Feats;

[ServiceBinding(typeof(OtherworldlyResilience))]
public class OtherworldlyResilience
{
    private const string ResilienceTag = "wlk_resilience";

    public OtherworldlyResilience()
    {
        NwModule.Instance.OnCreatureDamage += ApplyOnDamaged;
        NwModule.Instance.OnHeal += ApplyOnHealed;
    }

    private void ApplyOnDamaged(OnCreatureDamage eventData)
    {
        if (eventData.Target is not NwCreature creature || creature.WarlockLevel() < 8)
            return;

        AdjustResilience(creature);
    }

    private void ApplyOnHealed(OnHeal eventData)
    {
        if (eventData.Target is not NwCreature creature || creature.WarlockLevel() < 8)
            return;

        AdjustResilience(creature);
    }

    private static void AdjustResilience(NwCreature warlock)
    {
        bool belowHalfHp = warlock.HP < warlock.MaxHP / 2;

        Effect? resilience = warlock.ActiveEffects.FirstOrDefault(e => e.Tag == ResilienceTag);

        if (resilience != null)
        {
            if (!belowHalfHp)
                warlock.RemoveEffect(resilience);

            return;
        }

        if (!belowHalfHp) return;

        int regenAmount = warlock.WarlockLevel() switch
        {
            >= 8 and < 13 => 1,
            >= 13 and < 18 => 2,
            >= 18 and < 23 => 3,
            >= 23 and < 28 => 4,
            >= 28 => 5,
            _ => 0
        };

        if (regenAmount == 0) return;

        resilience = Effect.Regenerate(regenAmount, TimeSpan.FromSeconds(6));
        resilience.SubType = EffectSubType.Unyielding;
        resilience.Tag = ResilienceTag;

        warlock.ApplyEffect(EffectDuration.Permanent, resilience);
    }
}
