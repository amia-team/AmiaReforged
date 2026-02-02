using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.SwingingCenser;

[ServiceBinding(typeof(IAugmentation))]
public class KiShout : IAugmentation.ICastAugment
{
    public PathType Path { get; }
    public TechniqueType Technique { get; }
    public void ApplyCastAugmentation(NwCreature monk, OnSpellCast castData, BaseTechniqueCallback baseTechnique)
    {
        baseTechnique();
        AugmentKiShout(monk);
    }

    /// <summary>
    /// Ki Shout exhorts allies with +1 bonus to attack rolls for one turn, with an additional +1 bonus for every Ki Focus.
    /// </summary>
    private static void AugmentKiShout(NwCreature monk)
    {
        if (monk.Location == null) return;

        int abBonus = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 2,
            KiFocus.KiFocus2 => 3,
            KiFocus.KiFocus3 => 4,
            _ => 1
        };

        Effect abBonusEffect = Effect.LinkEffects(Effect.AttackIncrease(abBonus),
            Effect.VisualEffect(VfxType.DurCessatePositive));

        foreach (NwCreature ally in monk.Location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Colossal, false))
        {
            if (!monk.IsReactionTypeFriendly(ally)) continue;

            _ = ApplyBonusAb(monk, ally, abBonusEffect);
        }
    }

    private static async Task ApplyBonusAb(NwCreature monk, NwCreature ally, Effect abBonusEffect)
    {
        float delay = monk.Distance(ally) / 10;
        await NwTask.Delay(TimeSpan.FromSeconds(delay));

        ally.ApplyEffect(EffectDuration.Temporary, abBonusEffect, NwTimeSpan.FromRounds(3));
        ally.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpHeadHoly));
    }
}
