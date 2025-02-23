// Called from the spirit technique handler when the technique is cast
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;


namespace AmiaReforged.Classes.Monk.Techniques.Body;

public static class KiShout
{
    public static void CastKiShout(OnSpellCast castData)
    {
        NwCreature monk = (NwCreature)castData.Caster;
        PathType? path = MonkUtilFunctions.GetMonkPath(monk);
        const TechniqueType technique = TechniqueType.KiShout;

        if (path != null)
        {
            PathEffectApplier.ApplyPathEffects(path, technique, castData);
            return;
        }

        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;
        int dc = MonkUtilFunctions.CalculateMonkDc(monk);
        Effect kiShoutEffect = Effect.LinkEffects(Effect.Stunned(), Effect.VisualEffect(VfxType.DurCessateNegative));
        kiShoutEffect.SubType = EffectSubType.Supernatural;
        Effect kiShoutVfx = MonkUtilFunctions.ResizedVfx(VfxType.FnfHowlMind, RadiusSize.Large);
        TimeSpan effectDuration = NwTimeSpan.FromRounds(3);

        monk.ApplyEffect(EffectDuration.Instant, kiShoutVfx);
        foreach (NwGameObject nwObject in monk.Location!.GetObjectsInShape(Shape.Sphere, RadiusSize.Large, false))
        {
            NwCreature creatureInShape = (NwCreature)nwObject;
            if (!monk.IsReactionTypeHostile(creatureInShape)) continue;

            CreatureEvents.OnSpellCastAt.Signal(monk, creatureInShape, NwSpell.FromSpellType(Spell.AbilityQuiveringPalm)!);

            int damageAmount = Random.Shared.Roll(4, monkLevel);
            Effect damageEffect = Effect.LinkEffects(Effect.Damage(damageAmount, DamageType.Sonic), 
                Effect.VisualEffect(VfxType.ImpSonic));

            creatureInShape.ApplyEffect(EffectDuration.Instant, damageEffect);
            SavingThrowResult savingThrowResult = 
                creatureInShape.RollSavingThrow(SavingThrow.Will, dc, SavingThrowType.MindSpells, monk);
            
            if (savingThrowResult is SavingThrowResult.Success)
                creatureInShape.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpWillSavingThrowUse));
            
            if (savingThrowResult is SavingThrowResult.Failure)
                creatureInShape.ApplyEffect(EffectDuration.Temporary, kiShoutEffect, effectDuration);
        }
    }
}