using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using static AmiaReforged.Classes.Monk.Augmentations.CrashingMeteor.CrashingMeteorData;

namespace AmiaReforged.Classes.Monk.Augmentations.CrashingMeteor;

[ServiceBinding(typeof(IAugmentation))]
public class AugmentKiShout : IAugmentation.ICastAugment
{
    public PathType Path => PathType.CrashingMeteor;
    public TechniqueType Technique => TechniqueType.KiShout;

    /// <summary>
    /// Damage type matches the chosen element and inflicts 5% elemental vulnerability for 3 rounds.
    /// Each Ki Focus adds +5% vulnerability.
    /// </summary>
    public void ApplyCastAugmentation(NwCreature monk, OnSpellCast castData,
        BaseTechniqueCallback baseTechnique)
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

    private const string MeteorKiShoutTag = nameof(PathType.CrashingMeteor) + nameof(TechniqueType.KiShout);
}
