using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.SwingingCenser;

[ServiceBinding(typeof(IAugmentation))]
public class BindingStrike : IAugmentation.IDamageAugment
{
    public PathType Path => PathType.SwingingCenser;
    public TechniqueType Technique => TechniqueType.BindingStrike;
    public void ApplyDamageAugmentation(NwCreature monk, OnCreatureDamage damageData, BaseTechniqueCallback baseTechnique)
    {
        baseTechnique();
        AugmentBindingStrike(monk, damageData);
    }

    /// <summary>
    /// Binding Strike heals the monk or a nearby ally for 1d6 damage. Each Ki Focus heals for an additional 1d6, to a maximum of 4d6 damage.
    /// </summary>
    private static void AugmentBindingStrike(NwCreature monk, OnCreatureDamage damageData)
    {
        if (damageData.Target is not NwCreature targetCreature
            || !monk.IsReactionTypeHostile(targetCreature)
            || targetCreature.Location == null) return;

        NwCreature[] allies = targetCreature.Location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere,
                RadiusSize.Medium, true)
            .Where(c => monk.IsReactionTypeFriendly(c) && c.HP < c.MaxHP)
            .ToArray();

        if (allies.Length == 0) return;

        int healDice = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 2,
            KiFocus.KiFocus2 => 3,
            KiFocus.KiFocus3 => 4,
            _ => 1
        };

        Effect healPulseVfx = MonkUtils.ResizedVfx(MonkVfx.ImpPulseHolyChest, RadiusSize.Medium);
        targetCreature.ApplyEffect(EffectDuration.Instant, healPulseVfx);

        Effect healVfx = Effect.VisualEffect(VfxType.ImpHeadHeal, fScale: 0.7f);
        Effect healEffect = Effect.LinkEffects(Effect.Damage(healDice), healVfx);
        _ = MonkUtils.GetObjectContext(monk, healEffect);

        foreach (NwCreature ally in allies)
            ally.ApplyEffect(EffectDuration.Instant, healEffect);
    }
}
