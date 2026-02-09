using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Monk.Augmentations.EchoingValley;

[ServiceBinding(typeof(IAugmentation))]
public class AugmentBindingStrike : IAugmentation.IAttackAugment
{
    public PathType Path => PathType.EchoingValley;
    public TechniqueType Technique => TechniqueType.BindingStrike;

    /// <summary>
    /// Summons an Echo for 2 turns and causes existing Echoes to deal 1d6 sonic damage in a medium radius.
    /// Each Ki Focus allows one additional Echo.
    /// </summary>
    public void ApplyAttackAugmentation(NwCreature monk, OnCreatureAttack attackData, BaseTechniqueCallback baseTechnique)
    {
        baseTechnique();

        if (attackData.Target is not NwCreature targetCreature
            || !monk.IsReactionTypeHostile(targetCreature)) return;

        byte echoCap = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 2,
            KiFocus.KiFocus2 => 3,
            KiFocus.KiFocus3 => 4,
            _ => 1
        };

        NwCreature[] echoes = monk.Associates
            .Where(associate => associate.ResRef == EchoConstant.SummonResRef)
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
            Effect.SummonCreature(EchoConstant.SummonResRef, VfxType.ImpMagicProtection!, unsummonVfx: VfxType.ImpGrease);

        await monk.WaitForObjectContext();
        summonLocation.ApplyEffect(EffectDuration.Temporary, summonEcho, NwTimeSpan.FromTurns(2));

        await NwTask.Delay(TimeSpan.FromMilliseconds(1));

        NwCreature? newEcho = monk.Associates
            .FirstOrDefault(a => a.ResRef == EchoConstant.SummonResRef && !echoes.Contains(a));

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

    private static async Task EchoAoe(NwCreature monk, NwCreature echo)
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
}
