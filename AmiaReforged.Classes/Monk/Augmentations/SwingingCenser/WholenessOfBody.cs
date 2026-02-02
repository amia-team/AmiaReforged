using AmiaReforged.Classes.Monk.Types;
using AmiaReforged.Classes.Spells;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.SwingingCenser;

[ServiceBinding(typeof(IAugmentation))]
public class WholenessOfBody(ScriptHandleFactory scriptHandleFactory) : IAugmentation.ICastAugment
{
    private const string WholenessPulseTag = "wholeness_pulse";
    public PathType Path => PathType.SwingingCenser;
    public TechniqueType Technique => TechniqueType.WholenessOfBody;
    public void ApplyCastAugmentation(NwCreature monk, OnSpellCast castData, BaseTechniqueCallback baseTechnique)
    {
        AugmentWholenessOfBody(monk);
    }

    /// <summary>
    /// Wholeness of Body pulses in a large area around the monk, healing allies.
    /// Each Ki Focus adds another pulse of healing, to a maximum of four pulses.
    /// </summary>
    private void AugmentWholenessOfBody(NwCreature monk)
    {
        byte monkLevel = monk.GetClassInfo(ClassType.Monk)?.Level ?? 0;
        int healAmount = monkLevel * 2;

        int pulseAmount = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 2,
            KiFocus.KiFocus2 => 3,
            KiFocus.KiFocus3 => 4,
            _ => 1
        };

        TimeSpan duration = TimeSpan.FromSeconds((pulseAmount - 1) * 3);
        TimeSpan pulseInterval = TimeSpan.FromSeconds(3);

        Effect wholenessEffect = Effect.LinkEffects(Effect.Heal(healAmount),
            Effect.VisualEffect(VfxType.ImpHealingL, false, 0.7f));

        Effect pulseVfx = MonkUtils.ResizedVfx(VfxType.FnfLosHoly30, RadiusSize.Large);

        ScriptCallbackHandle doPulse
            = scriptHandleFactory.CreateUniqueHandler(_ => PulseHeal(monk, wholenessEffect, pulseVfx));

        Effect wholenessPulse = Effect.RunAction(doPulse, doPulse, doPulse,
            pulseInterval);

        monk.ApplyEffect(EffectDuration.Temporary, wholenessPulse, duration);
    }

    private static ScriptHandleResult PulseHeal(NwCreature monk, Effect wholenessEffect, Effect pulseVfx)
    {
        if (monk.IsDead || !monk.IsValid || monk.Location == null) return ScriptHandleResult.True;

        monk.ApplyEffect(EffectDuration.Instant, pulseVfx);

        foreach (NwCreature creature in monk.Location.GetObjectsInShapeByType<NwCreature>
                     (Shape.Sphere, RadiusSize.Large,false))
        {
            if (!monk.IsReactionTypeFriendly(creature)) continue;

            _ = ApplyWholenessEffect(creature, monk, wholenessEffect);
        }

        return ScriptHandleResult.True;
    }

    private static async Task ApplyWholenessEffect(NwCreature creature, NwCreature monk, Effect wholenessEffect)
    {
        TimeSpan randomDelay = SpellUtils.GetRandomDelay(0.4, 1.1);
        await NwTask.Delay(randomDelay);

        await monk.WaitForObjectContext();
        creature.ApplyEffect(EffectDuration.Instant, wholenessEffect);
    }
}
