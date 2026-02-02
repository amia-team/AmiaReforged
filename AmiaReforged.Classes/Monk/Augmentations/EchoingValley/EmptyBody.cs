using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.EchoingValley;

[ServiceBinding(typeof(IAugmentation))]
public class EmptyBody : IAugmentation.ICastAugment
{
    public PathType Path => PathType.EchoingValley;
    public TechniqueType Technique => TechniqueType.EmptyBody;

    public void ApplyCastAugmentation(NwCreature monk, OnSpellCast castData, BaseTechniqueCallback baseTechnique)
    {
        baseTechnique();
        AugmentEmptyBody(monk);
    }

    private const string EchoingEmptyBodyTag = nameof(PathType.EchoingValley) + nameof(TechniqueType.EmptyBody);

    /// <summary>
    /// Empty Body grants +1 bonus dodge AC for each Echo.
    /// </summary>
    private void AugmentEmptyBody(NwCreature monk)
    {
        int monkLevel = monk.GetClassInfo(ClassType.Monk)?.Level ?? 0;

        int echoCount = monk.Associates.Count(associate => associate.ResRef == EchoConstant.SummonResRef);

        Effect? emptyBodyEffect = monk.ActiveEffects.FirstOrDefault(e => e.Tag == EchoingEmptyBodyTag);
        if (emptyBodyEffect != null)
            monk.RemoveEffect(emptyBodyEffect);

        emptyBodyEffect = Effect.LinkEffects(
            Effect.ACIncrease(echoCount),
            Effect.VisualEffect(VfxType.DurPdkFear)
        );

        emptyBodyEffect.Tag = EchoingEmptyBodyTag;

        monk.ApplyEffect(EffectDuration.Temporary, emptyBodyEffect, NwTimeSpan.FromRounds(monkLevel));
    }
}
