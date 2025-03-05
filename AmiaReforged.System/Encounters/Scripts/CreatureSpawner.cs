using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.Encounters.Scripts;

public class CreatureSpawner
{
    private const string LocalVarNoSpawn = "no_spawn";
    private const int FifteenMinutesSeconds = 900;
    private readonly NwPlayer _player;
    private readonly NwTrigger _trigger;

    public CreatureSpawner(NwTrigger trigger, NwPlayer player)
    {
        _trigger = trigger;
        _player = player;
    }

    public void SpawnCreaturesForTrigger()
    {
        if (NWScript.GetLocalInt(_trigger.Area, LocalVarNoSpawn) == NWScript.TRUE)
            // Return, here. No spawns is enabled, so don't spawn anything.
            return;

        if (TriggerStillOnCooldown() && NWScript.GetLocalInt(_trigger, sVarName: "on_cooldown") == NWScript.TRUE)
        {
            _player.SendServerMessage(message: "You see signs of recent fighting here.");
            return;
        }

        DayNightEncounterSpawner spawner = new(_trigger);

        DetermineDoubleSpawns(spawner);

        spawner.SpawnEncounters();

        InitTriggerCooldown();

        NWScript.SetLocalInt(_trigger, sVarName: "on_cooldown", NWScript.TRUE);
        NWScript.DelayCommand(FifteenMinutesSeconds,
            () => NWScript.SetLocalInt(_trigger, sVarName: "on_cooldown", NWScript.FALSE));
    }

    private void DetermineDoubleSpawns(DayNightEncounterSpawner spawner)
    {
        IEnumerable<NwPlayer> partyMembers = _player.PartyMembers;

        if (partyMembers.Count(partyMember =>
                NWScript.GetArea(partyMember.LoginCreature) == NWScript.GetArea(_player.LoginCreature)) >
            6) spawner.IsDoubleSpawn = true;
    }

    private bool TriggerStillOnCooldown() => (int)DateTimeOffset.Now.ToUnixTimeSeconds() - GetTriggerCoolDownStart() <=
                                             FifteenMinutesSeconds;

    private int GetTriggerCoolDownStart() => NWScript.GetLocalInt(_trigger, sVarName: "cooldown_start");

    private void InitTriggerCooldown()
    {
        NWScript.SetLocalInt(_trigger, sVarName: "cooldown_start", (int)DateTimeOffset.Now.ToUnixTimeSeconds());
        NWScript.WriteTimestampedLogEntry($"{NWScript.GetLocalInt(_trigger, sVarName: "cooldown_start")}");
    }
}