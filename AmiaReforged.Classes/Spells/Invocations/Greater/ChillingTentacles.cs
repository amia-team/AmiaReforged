using AmiaReforged.Classes.Warlock;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Invocations.Greater;

[ServiceBinding(typeof(IInvocation))]
public class ChillingTentacles(ScriptHandleFactory scriptHandleFactory) : IInvocation
{
    private const int VfxPerChillingId = 51;
    public string ImpactScript => "wlk_chilltentac";
    public void CastInvocation(NwCreature warlock, int invocationCl, SpellEvents.OnSpellCast castData)
    {
        if (castData.TargetLocation is not { } location) return;

        int dc = warlock.InvocationDc(invocationCl);
        Effect paralyze = Effect.LinkEffects(Effect.Paralyze(), Effect.VisualEffect(VfxType.DurParalyzed));
        paralyze.SubType = EffectSubType.Magical;

        ScriptCallbackHandle onTentacleStrike = scriptHandleFactory.CreateUniqueHandler(info
            => OnTentacleStrike(info, warlock, invocationCl, dc, paralyze));
        Effect tentacleStrike = Effect.RunAction(onTentacleStrike);
        tentacleStrike.SubType = EffectSubType.Supernatural;

        ScriptCallbackHandle onHeartBeatChilling = scriptHandleFactory.CreateUniqueHandler(info
            => OnHeartbeatChilling(info, warlock, tentacleStrike, castData.Spell));

        PersistentVfxTableEntry chillingVfx = NwGameTables.PersistentEffectTable.GetRow(VfxPerChillingId);
        PersistentVfxTableEntry evardsVfx =
            NwGameTables.PersistentEffectTable.GetRow((int)PersistentVfxType.PerEvardsBlackTentacles);

        Effect chillingTentacles = Effect.LinkEffects
        (
            Effect.AreaOfEffect(chillingVfx, heartbeatHandle: onHeartBeatChilling),
            Effect.AreaOfEffect(evardsVfx)
        );
        chillingTentacles.SubType = EffectSubType.Magical;

        TimeSpan duration = NwTimeSpan.FromRounds(invocationCl);

        location.RemoveAoeSpell(warlock, castData.Spell, RadiusSize.Large);
        location.ApplyEffect(EffectDuration.Temporary, chillingTentacles, duration);
    }

    private static ScriptHandleResult OnHeartbeatChilling(CallInfo info, NwCreature warlock, Effect tentacleStrike,
        NwSpell spell)
    {
        if (!info.TryGetEvent(out AreaOfEffectEvents.OnHeartbeat? eventData)) return ScriptHandleResult.Handled;

        Effect impColdVfx = Effect.VisualEffect(VfxType.ImpFrostS);

        foreach (NwCreature creature in eventData.Effect.GetObjectsInEffectArea<NwCreature>())
        {
            if (!creature.IsValidInvocationTarget(warlock)) continue;

            CreatureEvents.OnSpellCastAt.Signal(warlock, creature, spell);
            creature.ApplyEffect(EffectDuration.Instant, tentacleStrike);
            int coldDamage = Random.Shared.Roll(6, 2);
            creature.ApplyEffect(EffectDuration.Instant, Effect.Damage(coldDamage, DamageType.Cold));
            creature.ApplyEffect(EffectDuration.Instant, impColdVfx);
        }

        return ScriptHandleResult.Handled;
    }

    private static ScriptHandleResult OnTentacleStrike(CallInfo info, NwCreature warlock, int invocationCl, int dc,
        Effect paralyze)
    {
        if (info.ObjectSelf is not NwCreature creature) return ScriptHandleResult.Handled;

        int tentacleCount = Random.Shared.Roll(4);

        int warlockBonus = invocationCl + warlock.GetAbilityModifier(Ability.Charisma);
        int creatureBonus = creature.BaseAttackBonus +
                            creature.GetAbilityModifier(Ability.Strength) +
                            SizeModifiers.GetValueOrDefault(creature.Size, 0);

        TimeSpan paralyzeDuration = NwTimeSpan.FromRounds(1);

        for (int i = 0; i < tentacleCount; i++)
        {
            if (GrappleCreature(warlockBonus, creatureBonus))
                _ = ApplyTentacle(warlock, dc, creature, paralyze, paralyzeDuration);
        }

        return ScriptHandleResult.Handled;
    }

    private static async Task ApplyTentacle(NwCreature warlock, int dc, NwCreature creature, Effect paralyze,
        TimeSpan paralyzeDuration)
    {
        await NwTask.Delay(SpellUtils.GetRandomDelay());
        await warlock.WaitForObjectContext();

        SavingThrowResult fortSave =
            creature.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.Paralysis, warlock);

        switch (fortSave)
        {
            case SavingThrowResult.Success:
                creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse));
                return;
            case SavingThrowResult.Failure:
                creature.ApplyEffect(EffectDuration.Temporary, paralyze, paralyzeDuration);
                break;
        }

        int damage = Random.Shared.Roll(6) + 4;
        creature.ApplyEffect(EffectDuration.Instant, Effect.Damage(damage, DamageType.Bludgeoning, DamagePower.Energy));
    }

    private static readonly Dictionary<CreatureSize, int> SizeModifiers = new()
    {
        { CreatureSize.Tiny, -8 },
        { CreatureSize.Small, -4 },
        { CreatureSize.Medium, 0 },
        { CreatureSize.Large, 4 },
        { CreatureSize.Huge, 8 }
    };

    private static bool GrappleCreature(int warlockBonus, int creatureBonus)
    {
        int warlockGrappleRoll = Random.Shared.Roll(20) + warlockBonus;
        int creatureGrappleRoll = Random.Shared.Roll(20) + creatureBonus;

        return creatureGrappleRoll <= warlockGrappleRoll;
    }
}
