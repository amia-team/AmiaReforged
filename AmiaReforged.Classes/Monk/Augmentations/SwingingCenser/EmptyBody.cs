using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.SwingingCenser;

[ServiceBinding(typeof(IAugmentation))]
public class EmptyBody : IAugmentation.ICastAugment
{
    public PathType Path => PathType.SwingingCenser;
    public TechniqueType Technique => TechniqueType.EmptyBody;
    public void ApplyCastAugmentation(NwCreature monk, OnSpellCast castData, BaseTechniqueCallback baseTechnique)
    {
        AugmentEmptyBody(monk);
    }

    /// <summary>
    /// Empty Body creates soothing winds in a large area around the monk, granting allies 50% concealment and
    /// 2 regeneration. Each Ki Focus increases the regeneration by 2, to a maximum of 8 regeneration.
    /// </summary>
    private static void AugmentEmptyBody(NwCreature monk)
    {
        byte monkLevel = monk.GetClassInfo(ClassType.Monk)?.Level ?? 0;
        const byte concealment = 50;

        int regen = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 4,
            KiFocus.KiFocus2 => 6,
            KiFocus.KiFocus3 => 8,
            _ => 2
        };

        Effect emptyBodyEffect = Effect.LinkEffects(
            Effect.Regenerate(regen, NwTimeSpan.FromRounds(1)),
            Effect.Concealment(concealment),
            Effect.VisualEffect(VfxType.DurInvisibility)
        );

        ApplyAoeEmptyBody(monk, emptyBodyEffect, NwTimeSpan.FromRounds(monkLevel));
    }

    private static void ApplyAoeEmptyBody(NwCreature monk, Effect emptyBodyEffect, TimeSpan effectDuration)
    {
        if (monk.Location == null) return;

        monk.Location.ApplyEffect(EffectDuration.Instant,
            Effect.VisualEffect(VfxType.FnfDispelGreater, false, 0.3f));

        foreach (NwGameObject nwObject in monk.Location.GetObjectsInShape(Shape.Sphere, RadiusSize.Large,
                     false))
        {
            NwCreature creatureInShape = (NwCreature)nwObject;

            if (!monk.IsReactionTypeFriendly(creatureInShape)) continue;

            creatureInShape.ApplyEffect(EffectDuration.Temporary, emptyBodyEffect, effectDuration);
        }
    }
}
