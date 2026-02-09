using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.EchoingValley;

[ServiceBinding(typeof(IAugmentation))]
public class AugmentEmptyBody : IAugmentation.ICastAugment
{
    public PathType Path => PathType.EchoingValley;
    public TechniqueType Technique => TechniqueType.EmptyBody;

    /// <summary>
    /// Grants +1 Dodge AC for each active Echo.
    /// </summary>
    public void ApplyCastAugmentation(NwCreature monk, OnSpellCast castData, BaseTechniqueCallback baseTechnique)
    {
        baseTechnique();

        int monkLevel = monk.GetClassInfo(ClassType.Monk)?.Level ?? 0;
        int echoCount = monk.Associates.Count(associate => associate.ResRef == EchoConstant.SummonResRef);

        Effect emptyBodyEffect = Effect.LinkEffects(
            Effect.ACIncrease(echoCount),
            Effect.VisualEffect(VfxType.DurPdkFear)
        );
        emptyBodyEffect.SubType = EffectSubType.Extraordinary;

        monk.ApplyEffect(EffectDuration.Temporary, emptyBodyEffect, NwTimeSpan.FromRounds(monkLevel));
    }
}
