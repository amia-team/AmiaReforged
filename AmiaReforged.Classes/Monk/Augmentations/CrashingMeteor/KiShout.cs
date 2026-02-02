using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using static AmiaReforged.Classes.Monk.Augmentations.CrashingMeteor.CrashingMeteorData;

namespace AmiaReforged.Classes.Monk.Augmentations.CrashingMeteor;

[ServiceBinding(typeof(IAugmentation))]
public class KiShout : IAugmentation.ICastAugment
{
    public PathType Path => PathType.CrashingMeteor;
    public TechniqueType Technique => TechniqueType.KiShout;

    public void ApplyCastAugmentation(NwCreature monk, OnSpellCast castData,
        BaseTechniqueCallback baseTechnique)
    {
        AugmentKiShout(monk);
    }

    private const string MeteorKiShoutTag = nameof(PathType.CrashingMeteor) + nameof(TechniqueType.KiShout);

    /// <summary>
    /// Ki Shout changes the damage from sonic to the chosen element. In addition, all enemies receive 5 %
    /// vulnerability to the element for three rounds, with every Ki Focus increasing it by 5 %, to a maximum of
    /// 20 % elemental damage vulnerability.
    /// </summary>
    private static void AugmentKiShout(NwCreature monk)
    {
        if (monk.Location == null) return;

        CrashingMeteorData meteor = GetCrashingMeteorData(monk);

        Techniques.Cast.KiShout.DoKiShout(monk, meteor.DamageType, meteor.DamageVfx);

        Effect elementalEffect = Effect.LinkEffects(Effect.DamageImmunityDecrease(meteor.DamageType, meteor.DamageVulnerability),
            Effect.VisualEffect(VfxType.DurCessateNegative));

        elementalEffect.SubType = EffectSubType.Extraordinary;
        elementalEffect.Tag = MeteorKiShoutTag;

        foreach (NwGameObject obj in monk.Location.GetObjectsInShape(Shape.Sphere, RadiusSize.Colossal, false))
        {
            if (obj is not NwCreature hostileCreature || !monk.IsReactionTypeHostile(hostileCreature)) continue;

            Effect? existingEffect = hostileCreature.ActiveEffects.FirstOrDefault(e => e.Tag == MeteorKiShoutTag);
            if (existingEffect != null)
                hostileCreature.RemoveEffect(existingEffect);

            hostileCreature.ApplyEffect(EffectDuration.Temporary, elementalEffect, NwTimeSpan.FromRounds(3));
        }
    }
}
