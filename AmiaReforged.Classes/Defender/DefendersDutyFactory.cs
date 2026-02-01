using System.Collections.Concurrent;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Defender;

[ServiceBinding(typeof(DefendersDutyFactory))]
public class DefendersDutyFactory
{
    /// <summary>
    ///     Thread-safe registry of active DefendersDuty instances, keyed by defender creature.
    /// </summary>
    private static readonly ConcurrentDictionary<NwCreature, DefendersDuty> ActiveDefenders = new();

    /// <summary>
    ///     Thread-safe registry tracking which creatures are currently being protected and by whom.
    ///     Key = protected creature, Value = defender creature protecting them.
    ///     First come, first served - a creature can only be protected by one defender at a time.
    /// </summary>
    private static readonly ConcurrentDictionary<NwCreature, NwCreature> ProtectedTargets = new();

    public DefendersDuty CreateDefendersDuty(NwPlayer defender)
        => new(defender);

    /// <summary>
    ///     Registers a DefendersDuty instance for a defender creature.
    /// </summary>
    public static void Register(NwCreature defender, DefendersDuty duty)
    {
        ActiveDefenders[defender] = duty;
    }

    /// <summary>
    ///     Unregisters a DefendersDuty instance for a defender creature.
    /// </summary>
    public static void Unregister(NwCreature defender)
    {
        ActiveDefenders.TryRemove(defender, out _);
    }

    /// <summary>
    ///     Attempts to retrieve an active DefendersDuty instance for a defender creature.
    /// </summary>
    public static bool TryGet(NwCreature defender, out DefendersDuty? duty)
    {
        return ActiveDefenders.TryGetValue(defender, out duty);
    }

    #region Protected Target Registry

    /// <summary>
    ///     Attempts to claim protection over a target creature.
    ///     Returns true if successful (target was not already protected).
    ///     Returns false if the target is already being protected by another defender.
    /// </summary>
    public static bool TryClaimProtection(NwCreature target, NwCreature defender)
    {
        return ProtectedTargets.TryAdd(target, defender);
    }

    /// <summary>
    ///     Releases protection over a target creature.
    ///     Only succeeds if the specified defender is the one currently protecting the target.
    /// </summary>
    public static bool ReleaseProtection(NwCreature target, NwCreature defender)
    {
        return ProtectedTargets.TryRemove(new KeyValuePair<NwCreature, NwCreature>(target, defender));
    }

    /// <summary>
    ///     Checks if a creature is currently being protected by any defender.
    /// </summary>
    public static bool IsBeingProtected(NwCreature target)
    {
        return ProtectedTargets.ContainsKey(target);
    }

    /// <summary>
    ///     Gets the defender currently protecting a target, if any.
    /// </summary>
    public static NwCreature? GetProtector(NwCreature target)
    {
        return ProtectedTargets.TryGetValue(target, out NwCreature? protector) ? protector : null;
    }

    /// <summary>
    ///     Releases all targets being protected by a specific defender.
    ///     Used when a defender's aura is deactivated or they disconnect.
    /// </summary>
    public static void ReleaseAllProtectedBy(NwCreature defender)
    {
        var targetsToRelease = ProtectedTargets
            .Where(kvp => kvp.Value == defender)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (NwCreature target in targetsToRelease)
        {
            ProtectedTargets.TryRemove(target, out _);
        }
    }

    #endregion
}
