using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.System.Services;

[ServiceBinding(typeof(ResetService))]
public class ResetService
{
    private readonly SchedulerService _schedulerService;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public ResetService(SchedulerService schedulerService)
    {
        _schedulerService = schedulerService;
        ScheduleAutosaveAndReset();
        
        NwModule.Instance.OnModuleLoad += SetInitialResetValue;
        NwModule.Instance.OnPlayerRest += DisplayResetTimer;

        Log.Info(
            $"Reset Service initialized. ");
    }

    private void ScheduleAutosaveAndReset()
    {
        _schedulerService.ScheduleRepeating(SavePCs, TimeSpan.FromMinutes(30));
		_schedulerService.ScheduleRepeating(DisplayResetTimer, TimeSpan.FromMinutes(15));
        _schedulerService.ScheduleRepeating(CheckShutdown, TimeSpan.FromMinutes(1));
    }

    private void SetInitialResetValue(ModuleEvents.OnModuleLoad obj)
    {
        NWScript.SetLocalFloat(NwModule.Instance, "minutesToReset", 480.0f);

        ResetTimeKeeperSingleton.Instance.ResetStartTime = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();
        Log.Info($"Initial restart time: {NWScript.GetLocalInt(NwModule.Instance, "hoursToReset")} hours");
    }

    private void DisplayResetTimer(ModuleEvents.OnPlayerRest obj)
    {
        //if (obj.RestEventType != RestEventType.Cancelled) return;

        float resetAtMinutes = NWScript.GetLocalFloat(NwModule.Instance, "minutesToReset");
        float uptime = (float)ResetTimeKeeperSingleton.Instance.Uptime() / 60;

        float estimatedReset = resetAtMinutes - uptime;
        obj.Player.SendServerMessage($"Estimated reset time: {(int) estimatedReset}", Color.FromRGBA("#5be9ffcc"));
    }

    private static void SavePCs()
    {
        foreach (NwPlayer instancePlayer in (IEnumerable<NwPlayer>)NwModule.Instance.Players)
        {
            if (instancePlayer.IsDM)
            {
                instancePlayer.SendServerMessage("-- DMs can't be saved. --");
                continue;
            }

            if (instancePlayer.LoginCreature.ActiveEffects.Any(e => e.EffectType == EffectType.Polymorph))
            {
                instancePlayer.SendServerMessage(
                    "-- Polymorphed PCs can't be saved. Please unpolymorph to save your PC. --");
                continue;
            }

            instancePlayer.SendServerMessage("-- Saving your PC now. --");
            NWScript.ExportSingleCharacter(instancePlayer.LoginCreature);
        }
    }

    private void CheckShutdown()
    {
        long uptime = ResetTimeKeeperSingleton.Instance.Uptime() / 60;
        float resetAtMinutes = NWScript.GetLocalFloat(NwModule.Instance, "minutesToReset");
        Log.Info($"Time since reset timer began: {((double)uptime / 60):0.00}");

        if (!(uptime >= resetAtMinutes)) return;

        ShutdownManager shutdown = new(_schedulerService);
        shutdown.InitiateShutdown();
    }
}