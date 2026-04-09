using AmiaReforged.Classes.Warlock;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Invocations.Pact;

[ServiceBinding(typeof(IInvocation))]
public class DancingPlague(ScriptHandleFactory scriptHandleFactory) : IInvocation
{
    private const VfxType DurPartyDust = (VfxType)2563;
    private const VfxType FnfPartyDust = (VfxType)2564;
    private const string FeySummonResRef = "wlkfey";

    public string ImpactScript => "wlk_dancingplag";
    public void CastInvocation(NwCreature warlock, int invocationCl, SpellEvents.OnSpellCast castData)
    {
        if (castData.TargetObject is not NwCreature targetCreature || targetCreature.Location is null) return;

        Effect fnfVfx = Effect.VisualEffect(FnfPartyDust);
        Effect durVfx = Effect.VisualEffect(DurPartyDust);
        Effect fortVfx = Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse);

        int dc = warlock.InvocationDc(invocationCl);
        NwSpell spell = castData.Spell;

        TimeSpan danceDuration = NwTimeSpan.FromRounds(1);
        Effect dancePlague = DancePlague(warlock, danceDuration, durVfx, spell, dc, fortVfx);
        dancePlague.SubType = EffectSubType.Supernatural;

        targetCreature.ApplyEffect(EffectDuration.Instant, fnfVfx);

        foreach (NwCreature creature in targetCreature.Location.GetObjectsInShapeByType<NwCreature>
                 (Shape.Sphere, RadiusSize.Medium, losCheck: true))
        {
            if (!creature.IsValidInvocationTarget(warlock, hurtSelf: false))
                continue;

            CreatureEvents.OnSpellCastAt.Signal(warlock, creature, spell);

            if (creature.HasSpellEffect(spell) || creature.IsImmuneTo(ImmunityType.Disease))
                continue;

            SavingThrowResult fortSave =
                creature.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.Disease, warlock);

            if (fortSave == SavingThrowResult.Success)
            {
                creature.ApplyEffect(EffectDuration.Temporary, fortVfx);
                continue;
            }

            creature.ApplyEffect(EffectDuration.Temporary, dancePlague, danceDuration);
        }

        if (warlock.HasPactCooldown()) return;

        VisualEffectTableEntry summonVfx = NwGameTables.VisualEffectTable.GetRow((int)VfxType.FnfSmokePuff);
        VisualEffectTableEntry unsummonVfx = NwGameTables.VisualEffectTable.GetRow((int)VfxType.FnfSummonMonster1);
        TimeSpan delay = TimeSpan.FromSeconds(1);
        Effect summonEffect = Effect.SummonCreature(FeySummonResRef, summonVfx, delay, unsummonVfx: unsummonVfx);
        TimeSpan summonDuration = WarlockExtensions.PactSummonDuration(invocationCl);
        Location summonLocation = targetCreature.Location;

        summonLocation.ApplyEffect(EffectDuration.Temporary, summonEffect, summonDuration);
        warlock.ApplyPactCooldown();

        _ = MakeFeyPretty(warlock, delay, durVfx, targetCreature);
        _ = MakeFeyDance(warlock, delay);
    }

    private static async Task MakeFeyPretty(NwCreature warlock, TimeSpan delay, Effect durVfx, NwCreature? creature)
    {
        await NwTask.Delay(delay);
        NwCreature? feySummon = warlock.Associates.FirstOrDefault(a => a.ResRef == FeySummonResRef);
        if (feySummon == null) return;

        feySummon.ApplyEffect(EffectDuration.Permanent, durVfx);
        feySummon.ApplyEffect(EffectDuration.Permanent, Effect.VisualEffect(VfxType.DurGlowLightGreen));

        if (creature == null) return;

        feySummon.FaceToObject(creature);
        feySummon.MovementRate = creature.MovementRate;
        feySummon.Appearance = creature.Appearance;
        feySummon.PortraitId = creature.PortraitId;

        foreach (CreaturePart part in Enum.GetValues<CreaturePart>())
        {
            feySummon.SetCreatureBodyPart(part, creature.GetCreatureBodyPart(part));
        }
        foreach (ColorChannel channel in Enum.GetValues<ColorChannel>())
        {
            feySummon.SetColor(channel, creature.GetColor(channel));
        }
    }

    private static async Task MakeFeyDance(NwCreature warlock, TimeSpan delay)
    {
        await NwTask.Delay(delay);

        NwCreature? feySummon = warlock.Associates.FirstOrDefault(a => a.ResRef == FeySummonResRef);
        if (feySummon == null) return;

        feySummon.ClearActionQueue();
        feySummon.PlayVoiceChat(VoiceChatType.BattleCry1);
        PlayRandomDance(feySummon, danceDuration: TimeSpan.FromSeconds(3));
    }

    private Effect DancePlague(NwCreature warlock, TimeSpan danceDuration, Effect durVfx, NwSpell spell, int dc, Effect fortVfx)
    {
        ScriptCallbackHandle onApplyDance = scriptHandleFactory.CreateUniqueHandler(info
            => OnApplyDance(info, danceDuration, durVfx, warlock, spell, dc, fortVfx));
        ScriptCallbackHandle onRemoveDance = scriptHandleFactory.CreateUniqueHandler(OnRemoveDance);

        Effect danceEffect = Effect.RunAction(onAppliedHandle: onApplyDance, onRemovedHandle: onRemoveDance);

        return danceEffect;
    }

    private ScriptHandleResult OnApplyDance(CallInfo info, TimeSpan danceDuration, Effect durVfx,
        NwCreature warlock, NwSpell spell, int dc, Effect fortVfx)
    {
        if (info.ObjectSelf is not NwCreature creature) return ScriptHandleResult.Handled;
        creature.ClearActionQueue();
        PlayRandomDance(creature, danceDuration);
        creature.ApplyEffect(EffectDuration.Temporary, durVfx, danceDuration);
        creature.Commandable = false;

        _ = SpreadDancePlague(warlock, source: creature, danceDuration, durVfx, dc, spell, fortVfx);

        return ScriptHandleResult.Handled;
    }

    private static void PlayRandomDance(NwCreature creature, TimeSpan danceDuration)
    {
        Animation randomDance = Random.Shared.Roll(2) == 1 ? Animation.LoopingSpasm : Animation.LoopingConjure2;
        creature.PlayAnimation(randomDance, animSpeed: 1.5f, duration: danceDuration);
    }

    private static ScriptHandleResult OnRemoveDance(CallInfo info)
    {
        if (info.ObjectSelf is not NwCreature creature) return ScriptHandleResult.Handled;

        creature.Commandable = true;
        creature.ClearActionQueue();
        return ScriptHandleResult.Handled;
    }

    private async Task SpreadDancePlague(NwCreature warlock, NwCreature source, TimeSpan danceDuration, Effect durVfx,
        int dc, NwSpell spell, Effect fortVfx)
    {
        await NwTask.Delay(TimeSpan.FromSeconds(3));
        await warlock.WaitForObjectContext();

        // Each time the Plague Spreads, the DC grows weaker and weaker
        dc--;

        Effect dancePlague = DancePlague(warlock, danceDuration, durVfx, spell, dc, fortVfx);

        if (source.Location is null) return;

        foreach (NwCreature creature in source.Location.GetObjectsInShapeByType<NwCreature>(
                     Shape.Sphere, RadiusSize.Medium, losCheck: true))
        {
            if (creature == source) continue;
            if (!creature.IsValidInvocationTarget(warlock, hurtSelf: false)) continue;

            CreatureEvents.OnSpellCastAt.Signal(warlock, creature, spell);

            if (creature.HasSpellEffect(spell) || creature.IsImmuneTo(ImmunityType.Disease))
                continue;

            SavingThrowResult fortSave =
                creature.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.Disease, warlock);

            if (fortSave == SavingThrowResult.Success)
            {
                creature.ApplyEffect(EffectDuration.Temporary, fortVfx);
                continue;
            }

            creature.ApplyEffect(EffectDuration.Temporary, dancePlague, danceDuration);
        }
    }
}
