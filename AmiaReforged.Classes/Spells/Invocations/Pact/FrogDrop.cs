using AmiaReforged.Classes.Warlock;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using static AmiaReforged.Classes.Warlock.PactSummon.Slaad.SlaadSummonData;

namespace AmiaReforged.Classes.Spells.Invocations.Pact;

[ServiceBinding(typeof(IInvocation))]
public class FrogDrop(ScriptHandleFactory scriptHandleFactory) : IInvocation
{
    private const VfxType VfxImpFrog = (VfxType)2565;

    public string ImpactScript => "wlk_frogdrop";
    public void CastInvocation(NwCreature warlock, int invocationCl, SpellEvents.OnSpellCast castData)
    {
        if (castData.TargetLocation is not { } location) return;

        Effect knockdown = Effect.LinkEffects(Effect.Knockdown(), Effect.VisualEffect(VfxType.ImpDazedS));
        Effect reflexVfx = Effect.VisualEffect(VfxType.ImpReflexSaveThrowUse);
        TimeSpan duration = NwTimeSpan.FromRounds(1);
        int dc = warlock.InvocationDc(invocationCl);

        location.ApplyEffect(EffectDuration.Temporary, Effect.VisualEffect(VfxType.ImpDustExplosion));
        location.ApplyEffect(EffectDuration.Temporary, Effect.VisualEffect(VfxType.FnfGasExplosionNature));

        foreach (NwCreature creature in location.GetObjectsInShapeByType<NwCreature>
                     (Shape.Sphere, RadiusSize.Medium, losCheck: true))
        {
            if (!creature.IsValidInvocationTarget(warlock, false)) continue;

            CreatureEvents.OnSpellCastAt.Signal(warlock, creature, castData.Spell);

            SavingThrowResult reflexSave =
                creature.RollSavingThrow(SavingThrow.Reflex, dc, SavingThrowType.Chaos, warlock);

            if (reflexSave == SavingThrowResult.Success)
            {
                creature.ApplyEffect(EffectDuration.Instant, reflexVfx);
                continue;
            }

            creature.ApplyEffect(EffectDuration.Temporary, knockdown, duration);
        }

        if (warlock.HasPactCooldown()) return;

        string frog = GetFrog(invocationCl);
        Effect summonFrog = Effect.SummonCreature(frog, VfxImpFrog!, unsummonVfx: VfxImpFrog);
        Effect frogDoll = FrogDoll(warlock);

        TimeSpan summonDuration = WarlockExtensions.PactSummonDuration(invocationCl);
        location.ApplyEffect(EffectDuration.Temporary, summonFrog, summonDuration);

        _ = ApplyFrogDoll(warlock, frog, frogDoll);

        warlock.ApplyPactCooldown();
    }

    private static async Task ApplyFrogDoll(NwCreature warlock, string frogResRef, Effect frogDoll)
    {
        await NwTask.Delay(TimeSpan.FromSeconds(0.1));

        NwCreature? summonedFrog = warlock.Associates.FirstOrDefault(a => a.ResRef == frogResRef);
        if (summonedFrog == null) return;

        summonedFrog.ApplyEffect(EffectDuration.Permanent, frogDoll);
    }

    private Effect FrogDoll(NwCreature warlock)
    {
        ScriptCallbackHandle summonNextFrog = scriptHandleFactory.CreateUniqueHandler(info
            => SummonNextFrog(info, warlock));

        Effect frogDoll = Effect.RunAction(onRemovedHandle: summonNextFrog);
        frogDoll.SubType = EffectSubType.Extraordinary;

        return frogDoll;
    }

    private ScriptHandleResult SummonNextFrog(CallInfo info, NwCreature warlock)
    {
        if (info.ObjectSelf is not NwCreature summonedFrog || summonedFrog.Location is null)
            return ScriptHandleResult.Handled;

        string nextFrog = GetNextFrogTier(summonedFrog.ResRef);
        if (string.IsNullOrEmpty(nextFrog)) return ScriptHandleResult.Handled;

        _ = ApplyFrogToMaster(warlock, nextFrog, summonedFrog.Location);

        Effect frogDoll = FrogDoll(warlock);
        _ = ApplyFrogDoll(warlock, nextFrog, frogDoll);

        return ScriptHandleResult.Handled;
    }

    private static async Task ApplyFrogToMaster(NwCreature warlock, string nextFrog, Location summonLocation)
    {
        await warlock.WaitForObjectContext();
        Effect nextFrogSummon = Effect.SummonCreature(nextFrog, VfxImpFrog!, unsummonVfx: VfxImpFrog);
        TimeSpan summonDuration = WarlockExtensions.PactSummonDuration(warlock.GetInvocationCasterLevel());
        summonLocation.ApplyEffect(EffectDuration.Temporary, nextFrogSummon, summonDuration);
    }

    private static string GetFrog(int invocationCl) => invocationCl switch
    {
        >= 1 and < 10 => RedSlaad,
        >= 10 and < 20 => BlueSlaad,
        >= 20 and < 30 => GreenSlaad,
        >= 30 => GraySlaad,
        _ => RedSlaad
    };
    private static string GetNextFrogTier(string frogTier) => frogTier switch
    {
        GraySlaad => GreenSlaad,
        GreenSlaad => BlueSlaad,
        BlueSlaad => RedSlaad,
        _ => string.Empty
    };
}
