using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Monk.Techniques.Body;
using AmiaReforged.Classes.Monk.Techniques.Martial;
using AmiaReforged.Classes.Monk.Techniques.Spirit;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Monk.Augmentations;

[ServiceBinding(typeof(IAugmentation))]
public sealed class EchoingValley : IAugmentation
{
    private const string SummonEchoResRef = "summon_echo";
    private const string EchoingEmptyBodyTag = "echoingvalley_emptybody";
    public PathType PathType => PathType.EchoingValley;
    public void ApplyAttackAugmentation(NwCreature monk, TechniqueType technique, OnCreatureAttack attackData)
    {
        switch (technique)
        {
            case TechniqueType.Stunning:
                AugmentStunningStrike(monk, attackData);
                break;
            case TechniqueType.Axiomatic:
                AugmentAxiomaticStrike(monk, attackData);
                break;
            case TechniqueType.Eagle:
                EagleStrike.DoEagleStrike(monk, attackData);
                break;
        }
    }

    public void ApplyCastAugmentation(NwCreature monk, TechniqueType technique, OnSpellCast castData)
    {
        switch (technique)
        {
            case TechniqueType.EmptyBody:
                AugmentEmptyBody(monk);
                break;
            case TechniqueType.KiShout:
                AugmentKiShout(monk);
                break;
            case TechniqueType.Wholeness:
                WholenessOfBody.DoWholenessOfBody(monk);
                break;
            case TechniqueType.KiBarrier:
                KiBarrier.DoKiBarrier(monk);
                break;
            case TechniqueType.Quivering:
                QuiveringPalm.DoQuiveringPalm(monk, castData);
                break;
        }
    }

    /// <summary>
    /// Stunning Strike summons an Echo and makes summoned Echoes deal 1d6 sonic damage in a medium radius.
    /// Echoes last for two turns. Each Ki Focus allows an additional Echo to be summoned.
    /// </summary>
    private void AugmentStunningStrike(NwCreature monk, OnCreatureAttack attackData)
    {
        StunningStrike.DoStunningStrike(attackData);

        if (attackData.Target is not NwCreature targetCreature || !targetCreature.IsReactionTypeHostile(monk)) return;

        byte echoCap = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 2,
            KiFocus.KiFocus2 => 3,
            KiFocus.KiFocus3 => 4,
            _ => 1
        };

        NwCreature[] echoes = monk.Associates
            .Where(associate => associate.ResRef == SummonEchoResRef)
            .ToArray();

        _ = SummonEcho(monk, echoCap, echoes);

        foreach (NwCreature echo in echoes)
        {
            echo.JumpToObject(targetCreature);

            EchoAoe(monk, echo);
        }
    }

    private async Task SummonEcho(NwCreature monk, byte echoCap, NwCreature[] echoes)
    {
        if (monk.Location == null) return;

        if (echoes.Length >= echoCap) return;

        Location? summonLocation = SummonUtility.GetRandomLocationAroundPoint(monk.Location, 3f);

        if (summonLocation is null) return;

        FeedbackPlugin.SetFeedbackMessageHidden(FeedbackPlugin.NWNX_FEEDBACK_ASSOCIATE_UNSUMMONING, 1, monk);

        Effect summonEcho =
            Effect.SummonCreature(SummonEchoResRef, VfxType.ImpMagicProtection!, unsummonVfx: VfxType.ImpGrease);

        await monk.WaitForObjectContext();
        summonLocation.ApplyEffect(EffectDuration.Temporary, summonEcho, NwTimeSpan.FromTurns(2));

        await NwTask.Delay(TimeSpan.FromMilliseconds(1));

        NwCreature? newEcho = monk.Associates
            .FirstOrDefault(a => a.ResRef == SummonEchoResRef && !echoes.Contains(a));

        if (newEcho != null)
            PacifyEcho(newEcho);

        FeedbackPlugin.SetFeedbackMessageHidden(FeedbackPlugin.NWNX_FEEDBACK_ASSOCIATE_UNSUMMONING, 0, monk);
    }

    private void PacifyEcho(NwCreature echo)
    {
        Effect echoEffect = Effect.LinkEffects(
            Effect.Pacified(),
            Effect.Ethereal()
        );
        echoEffect.SubType = EffectSubType.Unyielding;

        echo.ApplyEffect(EffectDuration.Permanent, echoEffect);
        echo.IsDestroyable = false;
    }

    private void EchoAoe(NwCreature monk, NwCreature echo)
    {
        if (echo.Location == null) return;

        Effect echoVfx = MonkUtils.ResizedVfx(VfxType.ImpBlindDeafM, RadiusSize.Medium);

        echo.Location.ApplyEffect(EffectDuration.Instant, echoVfx);

        foreach (NwGameObject nwObject in echo.Location.GetObjectsInShape(Shape.Sphere, RadiusSize.Medium, false))
        {
            if (nwObject is not NwCreature hostileCreature || !monk.IsReactionTypeHostile(hostileCreature)) continue;

            int damageAmount = Random.Shared.Roll(6);
            Effect damageEffect = Effect.Damage(damageAmount, DamageType.Sonic);

            hostileCreature.ApplyEffect(EffectDuration.Instant, damageEffect);
        }
    }

    /// <summary>
    /// Axiomatic Strike deals +1 bonus sonic damage for each Echo the monk has.
    /// </summary>
    private void AugmentAxiomaticStrike(NwCreature monk, OnCreatureAttack attackData)
    {
        AxiomaticStrike.DoAxiomaticStrike(attackData);

        NwCreature[] echoes = monk.Associates
            .Where(associate => associate.ResRef == SummonEchoResRef)
            .ToArray();

        if (echoes.Length == 0) return;

        DamageData<short> damageData = attackData.DamageData;
        short sonicDamage = damageData.GetDamageByType(DamageType.Sonic);

        sonicDamage += (short)echoes.Length;
        damageData.SetDamageByType(DamageType.Sonic, sonicDamage);
    }

    /// <summary>
    /// Empty Body grants +1 bonus dodge AC for each Echo.
    /// </summary>
    private void AugmentEmptyBody(NwCreature monk)
    {
        EmptyBody.DoEmptyBody(monk);

        int monkLevel = monk.GetClassInfo(ClassType.Monk)?.Level ?? 0;

        int echoCount = monk.Associates.Count(associate => associate.ResRef == SummonEchoResRef);

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

    /// <summary>
    /// Ki Shout releases the monk's Echoes, each Echo exploding and dealing 10d6 sonic damage in a large radius.
    /// If the target succeeds on a fortitude save, they take half damage and avoid being stunned for 1 round.
    /// </summary>
    private void AugmentKiShout(NwCreature monk)
    {
        KiShout.DoKiShout(monk);

        IEnumerable<NwCreature> echoes = monk.Associates
            .Where(associate => associate.ResRef == SummonEchoResRef);

        foreach (NwCreature echo in echoes)
        {
            float delay = echo.Distance(monk);
            _ = ReleaseEcho(monk, echo, delay);
        }
    }

    private async Task ReleaseEcho(NwCreature monk, NwCreature echo, float delay)
    {
        await NwTask.Delay(TimeSpan.FromSeconds(delay));

        if (echo.Location == null) return;

        ExplodeEcho(monk, echo);
        echo.IsDestroyable = true;
        echo.Destroy();
    }

    private void ExplodeEcho(NwCreature monk, NwCreature echo)
    {
        if (echo.Location == null) return;

        Effect explosionVfx = MonkUtils.ResizedVfx(VfxType.FnfMysticalExplosion, RadiusSize.Large);

        echo.Location.ApplyEffect(EffectDuration.Instant, explosionVfx);

        foreach (NwGameObject nwObject in echo.Location.GetObjectsInShape(Shape.Sphere, RadiusSize.Large, false))
        {
            if (nwObject is not NwCreature hostileCreature || !monk.IsReactionTypeHostile(hostileCreature)) continue;

            int dc = MonkUtils.CalculateMonkDc(monk);

            SavingThrowResult savingThrowResult =
                hostileCreature.RollSavingThrow(SavingThrow.Reflex, dc, SavingThrowType.Sonic, monk);

            int damageAmount = Random.Shared.Roll(6, 10);

            switch (savingThrowResult)
            {
                case SavingThrowResult.Success:
                    damageAmount /= 2;
                    hostileCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse));
                    break;
                case SavingThrowResult.Failure:
                    hostileCreature.ApplyEffect(EffectDuration.Temporary, Effect.Stunned(), NwTimeSpan.FromRounds(1));
                    break;
            }

            Effect damageEffect = Effect.LinkEffects(
                Effect.Damage(damageAmount, DamageType.Sonic),
                Effect.VisualEffect(VfxType.ImpSonic)
            );

            hostileCreature.ApplyEffect(EffectDuration.Instant, damageEffect);
        }
    }
}
