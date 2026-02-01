using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Monk.Techniques.Cast;
using AmiaReforged.Classes.Monk.Techniques.Attack;
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
    private const string EchoingEmptyBodyTag = nameof(PathType.EchoingValley) + nameof(TechniqueType.EmptyBody);
    public PathType PathType => PathType.EchoingValley;

    public void ApplyAttackAugmentation(NwCreature monk, OnCreatureAttack attackData)
    {
        AugmentAxiomaticStrike(monk, attackData);
    }

    public void ApplyDamageAugmentation(NwCreature monk, TechniqueType technique, OnCreatureDamage damageData)
    {
        switch (technique)
        {
            case TechniqueType.StunningStrike:
                AugmentStunningStrike(monk, damageData);
                break;
            case TechniqueType.EagleStrike:
                EagleStrike.DoEagleStrike(monk, damageData);
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
            case TechniqueType.WholenessOfBody:
                WholenessOfBody.DoWholenessOfBody(monk);
                break;
            case TechniqueType.KiBarrier:
                KiBarrier.DoKiBarrier(monk);
                break;
            case TechniqueType.QuiveringPalm:
                QuiveringPalm.DoQuiveringPalm(monk, castData);
                break;
        }
    }

    /// <summary>
    /// Stunning Strike summons an Echo and makes summoned Echoes deal 1d6 sonic damage in a medium radius.
    /// Echoes last for two turns. Each Ki Focus allows an additional Echo to be summoned.
    /// </summary>
    private void AugmentStunningStrike(NwCreature monk, OnCreatureDamage damageData)
    {
        StunningStrike.DoStunningStrike(damageData);

        if (damageData.Target is not NwCreature targetCreature || !targetCreature.IsReactionTypeHostile(monk)) return;

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

        _ = SummonEcho(monk, targetCreature, echoCap, echoes);

        foreach (NwCreature echo in echoes)
        {
            if (echo.Distance(targetCreature) > 3)
                echo.JumpToObject(targetCreature);

            _ = EchoAoe(monk, echo);
        }
    }

    private async Task SummonEcho(NwCreature monk, NwCreature targetCreature, byte echoCap, NwCreature[] echoes)
    {
        if (targetCreature.Location == null) return;

        if (echoes.Length >= echoCap) return;

        Location? summonLocation = SummonUtility.GetRandomLocationAroundPoint(targetCreature.Location, 2.5f);

        if (summonLocation is null) return;

        FeedbackPlugin.SetFeedbackMessageHidden(FeedbackPlugin.NWNX_FEEDBACK_ASSOCIATE_UNSUMMONING, 1, monk);

        foreach (NwCreature echo in echoes)
            echo.IsDestroyable = false;

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

        foreach (NwCreature echo in echoes)
            echo.IsDestroyable = true;
    }

    private void PacifyEcho(NwCreature echo)
    {
        Effect echoEffect = Effect.LinkEffects(
            Effect.Pacified(),
            Effect.Ethereal(),
            Effect.Immunity(ImmunityType.None) // This is actually immunity to all
        );
        echoEffect.SubType = EffectSubType.Unyielding;

        echo.ApplyEffect(EffectDuration.Permanent, echoEffect);
        echo.Immortal = true;
    }

    private async Task EchoAoe(NwCreature monk, NwCreature echo)
    {
        await NwTask.Delay(TimeSpan.FromMilliseconds(1));

        if (echo.Location == null) return;

        Effect echoVfx = MonkUtils.ResizedVfx(VfxType.ImpBlindDeafM, RadiusSize.Medium);

        echo.Location.ApplyEffect(EffectDuration.Instant, echoVfx);

        foreach (NwGameObject nwObject in echo.Location.GetObjectsInShape(Shape.Sphere, RadiusSize.Medium, false))
        {
            if (nwObject is not NwCreature hostileCreature || !monk.IsReactionTypeHostile(hostileCreature)) continue;

            int damageAmount = Random.Shared.Roll(6);

            await echo.WaitForObjectContext();
            Effect damageEffect = Effect.Damage(damageAmount, DamageType.Sonic);

            hostileCreature.ApplyEffect(EffectDuration.Instant, damageEffect);
        }
    }

    /// <summary>
    /// Axiomatic Strike deals +1 bonus sonic damage for each Echo the monk has.
    /// </summary>
    private void AugmentAxiomaticStrike(NwCreature monk, OnCreatureAttack attackData)
    {
        AxiomaticStrike.DoAxiomaticStrike(monk, attackData);

        AxiomaticStrike.DoAxiomaticStrike(monk, attackData);

        DamageData<short> damageData = attackData.DamageData;
        short sonicDamage = damageData.GetDamageByType(DamageType.Sonic);

        int bonusDamage = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 2,
            KiFocus.KiFocus2 => 3,
            KiFocus.KiFocus3 => 4,
            _ => 1
        };

        if (attackData.AttackResult == AttackResult.CriticalHit)
            bonusDamage *= MonkUtils.GetCritMultiplier(attackData, monk);

        if (sonicDamage == -1) bonusDamage++;

        sonicDamage += (short)bonusDamage;
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

        if (monk.Location == null) return;

        foreach (NwCreature echo in monk.Location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere,
                     RadiusSize.Colossal, false))
        {
            if (echo.Master == monk && echo.ResRef == SummonEchoResRef)
            {
                _ = ExplodeEcho(monk, echo);
            }
        }

    }

    private async Task ExplodeEcho(NwCreature monk, NwCreature echo)
    {
        float delay = monk.Distance(echo) / 10;
        await NwTask.Delay(TimeSpan.FromSeconds(delay));

        Effect explosionVfx = MonkUtils.ResizedVfx(VfxType.FnfMysticalExplosion, RadiusSize.Large);
        if (echo.Location == null) return;

        echo.Location.ApplyEffect(EffectDuration.Instant, explosionVfx);

        foreach (NwCreature hostileCreature in echo.Location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Large, false))
        {
            if (!monk.IsReactionTypeHostile(hostileCreature)) continue;

            int dc = MonkUtils.CalculateMonkDc(monk);

            SavingThrowResult savingThrowResult =
                hostileCreature.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.Sonic, monk);

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

            await echo.WaitForObjectContext();
            Effect damageEffect = Effect.LinkEffects(
                Effect.Damage(damageAmount, DamageType.Sonic),
                Effect.VisualEffect(VfxType.ImpSonic)
            );

            hostileCreature.ApplyEffect(EffectDuration.Instant, damageEffect);
        }

        echo.Destroy();
    }
}
