using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Associates;

/// <summary>
///     Periodically restores all special ability uses for summoned familiars and animal companions.
///     Scheduled tasks are disposed when the associate is unsummoned, killed, or the owner disconnects.
/// </summary>
[ServiceBinding(typeof(AssociateAbilityRechargeService))]
public class AssociateAbilityRechargeService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    ///     Recharge interval – once per minute.
    /// </summary>
    private static readonly TimeSpan RechargeInterval = TimeSpan.FromSeconds(60);

    private readonly SchedulerService _scheduler;

    /// <summary>
    ///     Maps each tracked associate to its repeating recharge task.
    /// </summary>
    private readonly Dictionary<NwCreature, ScheduledTask> _activeSchedules = new();

    /// <summary>
    ///     Maps each owner to the set of associates currently being tracked, so we can clean up on
    ///     <see cref="ModuleEvents.OnClientLeave" /> even if <see cref="OnAssociateRemove" /> doesn't fire.
    /// </summary>
    private readonly Dictionary<NwCreature, HashSet<NwCreature>> _ownerAssociates = new();

    /// <summary>
    ///     Stale keys awaiting removal – avoids mutating dictionaries while iterating.
    /// </summary>
    private readonly List<NwCreature> _staleKeys = new();

    public AssociateAbilityRechargeService(SchedulerService scheduler)
    {
        _scheduler = scheduler;

        NwModule.Instance.OnAssociateAdd += OnAssociateAdded;
        NwModule.Instance.OnAssociateRemove += OnAssociateRemoved;
        NwModule.Instance.OnClientLeave += OnClientLeave;

        Log.Info("Associate Ability Recharge Service initialized (interval: 60 s).");
    }


    private void OnAssociateAdded(OnAssociateAdd eventData)
    {
        if (eventData.AssociateType is not (AssociateType.Familiar or AssociateType.AnimalCompanion))
            return;

        NwCreature? owner = eventData.Owner;
        NwCreature? associate = eventData.Associate;

        if (owner is null || associate is null) return;

        // Purge any stale keys from previous ticks before inserting.
        PurgeStaleKeys();

        // Avoid double-scheduling if the same creature is somehow added twice.
        if (_activeSchedules.ContainsKey(associate)) return;

        ScheduledTask task = _scheduler.ScheduleRepeating(
            () => Recharge(associate), RechargeInterval, RechargeInterval);

        _activeSchedules[associate] = task;

        // Track owner → associate mapping for OnClientLeave cleanup.
        if (!_ownerAssociates.TryGetValue(owner, out HashSet<NwCreature>? set))
        {
            set = new HashSet<NwCreature>();
            _ownerAssociates[owner] = set;
        }

        set.Add(associate);

        // Subscribe to death so we can cancel immediately.
        associate.OnDeath += OnAssociateDeath;

        Log.Info(
            $"Ability recharge scheduled for {associate.Name} ({eventData.AssociateType}, master: {owner.Name}).");
    }

    private void OnAssociateDeath(CreatureEvents.OnDeath obj)
    {
        NwCreature? associate = obj.KilledCreature;
        if (associate is null) return;

        associate.OnDeath -= OnAssociateDeath;
        CancelSchedule(associate);
    }

    private void OnAssociateRemoved(OnAssociateRemove obj)
    {
        NwCreature? associate = obj.Associate;
        if (associate is null) return;

        if (!_activeSchedules.ContainsKey(associate)) return;

        associate.OnDeath -= OnAssociateDeath;
        CancelSchedule(associate);
    }

    /// <summary>
    ///     Safety net: if the owner disconnects and <see cref="OnAssociateRemove" /> doesn't fire for every
    ///     associate, we clean them all up here.
    /// </summary>
    private void OnClientLeave(ModuleEvents.OnClientLeave obj)
    {
        NwCreature? owner = obj.Player?.LoginCreature;
        if (owner is null) return;

        if (!_ownerAssociates.TryGetValue(owner, out HashSet<NwCreature>? associates)) return;

        foreach (NwCreature associate in associates)
        {
            if (associate is not null && associate.IsValid)
                associate.OnDeath -= OnAssociateDeath;

            if (_activeSchedules.Remove(associate!, out ScheduledTask? task))
            {
                task.Dispose();
                string name = associate is not null && associate.IsValid ? associate.Name : "<invalid>";
                Log.Info($"Ability recharge cancelled for {name} (owner disconnected).");
            }
        }

        _ownerAssociates.Remove(owner);
    }


    /// <summary>
    ///   Invoked on each recharge tick for each associate. If the associate is invalid or dead, cancels the schedule.
    /// </summary>
    /// <param name="associate"></param>
    private void Recharge(NwCreature? associate)
    {
        if (associate is null || !associate.IsValid || associate.IsDead)
        {
            if (associate is not null) CancelSchedule(associate);
            return;
        }

        CreaturePlugin.RestoreSpecialAbilities(associate);
    }

    /// <summary>
    ///   Cancels the recharge schedule for the specified associate and cleans up all related mappings. Safe to call multiple times or with invalid creatures. Logs the cancellation for debugging purposes.
    /// </summary>
    /// <param name="associate"></param>
    private void CancelSchedule(NwCreature associate)
    {
        if (!_activeSchedules.Remove(associate, out ScheduledTask? task)) return;

        task.Dispose();

        // Clean up the owner mapping – collect stale owner keys to remove after iteration.
        NwCreature? ownerToRemove = null;
        foreach ((NwCreature owner, HashSet<NwCreature> set) in _ownerAssociates)
        {
            if (!set.Remove(associate)) continue;
            if (set.Count == 0) ownerToRemove = owner;
            break;
        }

        if (ownerToRemove is not null) _ownerAssociates.Remove(ownerToRemove);

        string name = associate.IsValid ? associate.Name : "<invalid>";
        Log.Info($"Ability recharge cancelled for {name}.");
    }

    /// <summary>
    ///     Removes entries whose <see cref="NwCreature" /> keys have become invalid since they were inserted.
    /// </summary>
    private void PurgeStaleKeys()
    {
        _staleKeys.Clear();

        foreach (NwCreature key in _activeSchedules.Keys)
        {
            if (!key.IsValid) _staleKeys.Add(key);
        }

        foreach (NwCreature stale in _staleKeys)
        {
            if (_activeSchedules.Remove(stale, out ScheduledTask? task))
                task.Dispose();
        }

        _staleKeys.Clear();
    }
}
