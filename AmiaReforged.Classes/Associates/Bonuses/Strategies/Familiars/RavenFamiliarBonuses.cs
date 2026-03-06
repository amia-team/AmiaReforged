using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;
using SpecialAbility = Anvil.API.SpecialAbility;

namespace AmiaReforged.Classes.Associates.Bonuses.Strategies.Familiars;

[ServiceBinding(typeof(IFamiliarBonusStrategy))]
public class RavenFamiliarBonuses : IFamiliarBonusStrategy
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// One NWN combat round/turn = 6 seconds in real time.
    /// </summary>
    private static readonly TimeSpan OneRound = TimeSpan.FromSeconds(6);

    private readonly SchedulerService _scheduler;

    /// <summary>
    /// Tracks the scheduled recharge task per raven so it can be cancelled on death/unsummon.
    /// </summary>
    private readonly Dictionary<NwCreature, ScheduledTask> _activeSchedules = new();

    public string ResRefPrefix => "nw_fm_rave";

    public RavenFamiliarBonuses(SchedulerService scheduler)
    {
        _scheduler = scheduler;

        // Clean up scheduled tasks when any associate is removed (unsummon, dismiss, etc.)
        NwModule.Instance.OnAssociateRemove += OnAssociateRemoved;
    }

    public void Apply(NwCreature owner, NwCreature associate)
    {
        switch (associate.Level)
        {
            case >= 5 and < 10:
                NwSpell? fromSpellType = NwSpell.FromSpellType(Spell.AbilityHowlFear);
                if (fromSpellType is null) return;

                SpecialAbility specialAbility = new SpecialAbility(fromSpellType, (byte)associate.Level);
                associate.AddSpecialAbility(specialAbility);
                break;
        }

        // Schedule ability recharge every round, with a one-round initial delay.
        ScheduledTask task = _scheduler.ScheduleRepeating(() => RechargeAbilities(associate), OneRound, OneRound);
        _activeSchedules[associate] = task;

        // Also cancel on death in case death fires before the associate-remove event.
        associate.OnDeath += OnRavenDeath;

        Log.Info($"Raven familiar recharge scheduled for {associate.Name} (master: {owner.Name}).");
    }

    private void RechargeAbilities(NwCreature raven)
    {
        // Guard: if the creature is invalid or dead, cancel the schedule.
        if (!raven.IsValid || raven.IsDead)
        {
            CancelSchedule(raven);
            return;
        }

        Effect vfx = Effect.VisualEffect(VfxType.ImpPdkFinalStand);
        raven.ApplyEffect(EffectDuration.Instant, vfx);
        CreaturePlugin.RestoreSpecialAbilities(raven);
    }

    private void OnRavenDeath(CreatureEvents.OnDeath obj)
    {
        NwCreature raven = obj.KilledCreature;
        raven.OnDeath -= OnRavenDeath;
        CancelSchedule(raven);
    }

    private void OnAssociateRemoved(OnAssociateRemove obj)
    {
        // Only care about familiars we're tracking.
        if (!_activeSchedules.ContainsKey(obj.Associate)) return;

        obj.Associate.OnDeath -= OnRavenDeath;
        CancelSchedule(obj.Associate);
    }

    private void CancelSchedule(NwCreature raven)
    {
        if (!_activeSchedules.Remove(raven, out ScheduledTask? task)) return;

        task.Dispose();
        Log.Info($"Raven familiar recharge cancelled for {raven.Name}.");
    }
}
