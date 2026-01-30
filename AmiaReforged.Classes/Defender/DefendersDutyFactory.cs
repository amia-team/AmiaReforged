﻿using System.Collections.Concurrent;
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
}
