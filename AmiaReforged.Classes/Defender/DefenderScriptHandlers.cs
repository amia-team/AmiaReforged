﻿using Anvil.API;
using Anvil.Services;
using NLog;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Defender;

[ServiceBinding(typeof(DefenderScriptHandlers))]
public class DefenderScriptHandlers
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly DefendersDutyFactory _abilityFactory;

    public DefenderScriptHandlers(DefendersDutyFactory abilityFactory)
    {
        _abilityFactory = abilityFactory;
        Log.Info("Setup Defender Script Handlers.");
    }

    /// <summary>
    ///     Script handler for defenders duty. Toggles the Defender's Duty aura which:
    ///     - Protects friendly creatures in the aura (absorbs 50% of their damage)
    ///     - Taunts hostile creatures in the aura (Concentration check vs Taunt skill)
    /// </summary>
    [ScriptHandler(scriptName: "def_duty")]
    public void OnDefendersDuty(CallInfo info)
    {
        if (info.ObjectSelf == null)
        {
            Log.Warn("Defenders Duty called with no object self.");
            return;
        }

        if (!info.ObjectSelf.IsPlayerControlled(out NwPlayer? player)) return;

        DefendersDuty duty = _abilityFactory.CreateDefendersDuty(player);
        duty.Apply();
    }

    /// <summary>
    ///     Script handler for Defender's Duty AOE enter event.
    ///     Called when a creature enters the aura.
    /// </summary>
    [ScriptHandler(scriptName: "def_duty_enter")]
    public void OnDefendersDutyEnter(CallInfo info)
    {
        uint aoeObject = info.ObjectSelf;
        uint creatorId = GetAreaOfEffectCreator(aoeObject);

        NwCreature? defender = creatorId.ToNwObject<NwCreature>();
        if (defender == null || !defender.IsValid)
        {
            Log.Warn("def_duty_enter: Could not find valid defender from AOE creator.");
            return;
        }

        if (!DefendersDutyFactory.TryGet(defender, out DefendersDuty? duty) || duty == null)
        {
            Log.Warn($"def_duty_enter: No DefendersDuty instance found for {defender.Name}.");
            return;
        }

        uint enteringId = GetEnteringObject();
        NwCreature? enteringCreature = enteringId.ToNwObject<NwCreature>();
        if (enteringCreature == null || !enteringCreature.IsValid)
            return;

        duty.OnEnterAura(enteringCreature);
    }

    /// <summary>
    ///     Script handler for Defender's Duty AOE heartbeat event.
    ///     Called every 6 seconds while the aura is active.
    /// </summary>
    [ScriptHandler(scriptName: "def_duty_hb")]
    public void OnDefendersDutyHeartbeat(CallInfo info)
    {
        uint aoeObject = info.ObjectSelf;
        uint creatorId = GetAreaOfEffectCreator(aoeObject);

        NwCreature? defender = creatorId.ToNwObject<NwCreature>();
        if (defender == null || !defender.IsValid)
            return;

        if (!DefendersDutyFactory.TryGet(defender, out DefendersDuty? duty) || duty == null)
            return;

        NwAreaOfEffect? aoe = aoeObject.ToNwObject<NwAreaOfEffect>();
        if (aoe == null || !aoe.IsValid)
            return;

        duty.OnHeartbeatAura(aoe);
    }

    /// <summary>
    ///     Script handler for Defender's Duty AOE exit event.
    ///     Called when a creature exits the aura.
    /// </summary>
    [ScriptHandler(scriptName: "def_duty_exit")]
    public void OnDefendersDutyExit(CallInfo info)
    {
        uint aoeObject = info.ObjectSelf;
        uint creatorId = GetAreaOfEffectCreator(aoeObject);

        NwCreature? defender = creatorId.ToNwObject<NwCreature>();
        if (defender == null || !defender.IsValid)
            return;

        if (!DefendersDutyFactory.TryGet(defender, out DefendersDuty? duty) || duty == null)
            return;

        uint exitingId = GetExitingObject();
        NwCreature? exitingCreature = exitingId.ToNwObject<NwCreature>();
        if (exitingCreature == null || !exitingCreature.IsValid)
            return;

        duty.OnExitAura(exitingCreature);
    }
}
