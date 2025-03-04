// Called from the spirit technique handler when the technique is cast

using AmiaReforged.Classes.Monk.Augmentations;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;


namespace AmiaReforged.Classes.Monk.Techniques.Spirit;

public static class KiShout
{
    public static void CastKiShout(OnSpellCast castData)
    {
        NwCreature monk = (NwCreature)castData.Caster;
        PathType? path = MonkUtilFunctions.GetMonkPath(monk);
        const TechniqueType technique = TechniqueType.KiShout;

        if (path != null)
        {
            AugmentationApplier.ApplyAugmentations(path, technique, castData);
            return;
        }
        
        DoKiShout(castData);
    }

    public static void DoKiShout(OnSpellCast castData)
    {
        NwCreature monk = (NwCreature)castData.Caster;
        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;
        int dc = MonkUtilFunctions.CalculateMonkDc(monk);
        Effect kiShoutEffect = Effect.Stunned();
        kiShoutEffect.SubType = EffectSubType.Supernatural;
        Effect kiShoutVfx = Effect.VisualEffect(VfxType.FnfHowlMind);
        TimeSpan effectDuration = NwTimeSpan.FromRounds(3);

        monk.ApplyEffect(EffectDuration.Instant, kiShoutVfx);
        foreach (NwGameObject nwObject in monk.Location!.GetObjectsInShape(Shape.Sphere, RadiusSize.Colossal, false))
        {
            NwCreature creatureInShape = (NwCreature)nwObject;
            if (!monk.IsReactionTypeHostile(creatureInShape)) continue;

            CreatureEvents.OnSpellCastAt.Signal(monk, creatureInShape, NwSpell.FromSpellType(Spell.AbilityHowlSonic)!);

            int damageAmount = Random.Shared.Roll(4, monkLevel);
            Effect damageEffect = Effect.LinkEffects(Effect.Damage(damageAmount, DamageType.Sonic),
                Effect.VisualEffect(VfxType.ImpSonic));

            creatureInShape.ApplyEffect(EffectDuration.Instant, damageEffect);
            
            SavingThrowResult savingThrowResult =
                creatureInShape.RollSavingThrow(SavingThrow.Will, dc, SavingThrowType.MindSpells, monk);

            if (savingThrowResult is SavingThrowResult.Failure)
            {
                creatureInShape.ApplyEffect(EffectDuration.Temporary, kiShoutEffect, effectDuration);
                continue;
            }
            
            creatureInShape.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpWillSavingThrowUse));
        }
    }
}