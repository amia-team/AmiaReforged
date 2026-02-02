using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.FloatingLeaf;

[ServiceBinding(typeof(IAugmentation))]
public class EmptyBody : IAugmentation.ICastAugment
{
    private const string FloatingEmptyBodyTag = nameof(PathType.FloatingLeaf) + nameof(TechniqueType.EmptyBody);
    private const string FLoatingLeapTag = nameof(PathType.FloatingLeaf) + "Leaping";
    public PathType Path => PathType.FloatingLeaf;
    public TechniqueType Technique => TechniqueType.EmptyBody;
    public void ApplyCastAugmentation(NwCreature monk, OnSpellCast castData, BaseTechniqueCallback baseTechnique)
    {
        baseTechnique();
        AugmentEmptyBody(monk);
    }

    /// <summary>
    /// While affected by Empty Body, Ki Focus I grants the ability to take weightless leaps.
    /// Ki Focus II grants haste. Ki Focus III grants Epic Dodge.
    /// </summary>
    private static void AugmentEmptyBody(NwCreature monk)
    {
        KiFocus? kiFocus = MonkUtils.GetKiFocus(monk);
        if (kiFocus == null) return;

        byte monkLevel = monk.GetClassInfo(ClassType.Monk)?.Level ?? 0;

        Effect? emptyBodyEffect = null;

        switch (kiFocus)
        {
            case KiFocus.KiFocus1:
                emptyBodyEffect = Effect.VisualEffect(VfxType.None);
                break;
            case KiFocus.KiFocus2:
                emptyBodyEffect = Effect.Haste();
                break;
            case KiFocus.KiFocus3:
                emptyBodyEffect = Effect.LinkEffects(Effect.Haste(), Effect.BonusFeat(Feat.EpicDodge!));
                break;
        }

        if (emptyBodyEffect == null) return;
        emptyBodyEffect.Tag = FloatingEmptyBodyTag;

        monk.ApplyEffect(EffectDuration.Temporary, emptyBodyEffect, NwTimeSpan.FromRounds(monkLevel));
    }

}
