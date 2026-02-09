using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.SwingingCenser;

[ServiceBinding(typeof(IAugmentation))]
public class AugmentBindingStrike : IAugmentation.IDamageAugment
{
    public PathType Path => PathType.SwingingCenser;
    public TechniqueType Technique => TechniqueType.BindingStrike;

    /// <summary>
    /// Heals the monk and nearby allies for 1d6 in a medium radius. Each Ki Focus adds +1d6.
    /// </summary>
    public void ApplyDamageAugmentation(NwCreature monk, OnCreatureDamage damageData, BaseTechniqueCallback baseTechnique)
    {
        baseTechnique();

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
