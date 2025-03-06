using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.System.Services;

[ServiceBinding(typeof(ResetService))]
public class ResetService
{
    private const string ResetTimerLVar = "minutesToReset";
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly SchedulerService _schedulerService;
    private readonly ShutdownManager _shutdownManager;

    public ResetService(SchedulerService schedulerService, ShutdownManager shutdownManager)
    {
        _schedulerService = schedulerService;
        _shutdownManager = shutdownManager;

        ScheduleAutosaveAndReset();

        NwModule.Instance.OnModuleLoad += SetInitialResetValue;
        NwModule.Instance.OnPlayerRest += DisplayResetTimer;

        Log.Info(
            "Reset Service initialized. ");
    }

    private void ScheduleAutosaveAndReset()
    {
        _schedulerService.ScheduleRepeating(LogRemainingReset, TimeSpan.FromMinutes(10));
        _schedulerService.ScheduleRepeating(SavePCs, TimeSpan.FromMinutes(30));
        _schedulerService.ScheduleRepeating(CheckShutdown, TimeSpan.FromMinutes(1));
    }

    private void LogRemainingReset()
    {
        float resetAtMinutes = NWScript.GetLocalFloat(NwModule.Instance, ResetTimerLVar);
        // Calculate time until reset in minutes
        float uptime = (float)ResetTimeKeeperSingleton.Instance.Uptime() / 60;
        float estimatedReset = resetAtMinutes - uptime;
        Log.Info($"Estimated time until reset: {(int)estimatedReset}");

        // Tell everyone in the server how long until reset
        NwModule.Instance.Players.ToList().ForEach(p =>
        {
            p.SendServerMessage($"Estimated time until reset: {(int)estimatedReset}",
                Color.FromRGBA(rgbaHexString: "#e6e600"));
        });
    }

    private void SetInitialResetValue(ModuleEvents.OnModuleLoad obj)
    {
        NWScript.SetLocalFloat(NwModule.Instance, ResetTimerLVar, 480.0f);

        ResetTimeKeeperSingleton.Instance.ResetStartTime = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();
        Log.Info($"Initial restart time: {NWScript.GetLocalInt(NwModule.Instance, sVarName: "hoursToReset")} hours");
    }

    private void DisplayResetTimer(ModuleEvents.OnPlayerRest obj)
    {
        if (obj.RestEventType != RestEventType.Cancelled) return;

        float resetAtMinutes = NWScript.GetLocalFloat(NwModule.Instance, ResetTimerLVar);
        float uptime = (float)ResetTimeKeeperSingleton.Instance.Uptime() / 60;

        float estimatedReset = resetAtMinutes - uptime;
        obj.Player.SendServerMessage($"Estimated reset time: {(int)estimatedReset}",
            Color.FromRGBA(rgbaHexString: "#5be9ffcc"));
    }

    private static void SavePCs()
    {
        foreach (NwPlayer instancePlayer in NwModule.Instance.Players)
        {
            if (instancePlayer.IsDM)
            {
                instancePlayer.SendServerMessage(message: "-- DMs can't be saved. --");
                continue;
            }

            if (instancePlayer.LoginCreature!.ActiveEffects.Any(e => e.EffectType == EffectType.Polymorph))
            {
                instancePlayer.SendServerMessage(
                    message: "-- Polymorphed PCs can't be saved. Please unpolymorph to save your PC. --");
                continue;
            }

            instancePlayer.SendServerMessage(message: "-- Saving your PC now. --");
            NWScript.ExportSingleCharacter(instancePlayer.LoginCreature);
        }
    }

    private void CheckShutdown()
    {
        float uptime = (float)ResetTimeKeeperSingleton.Instance.Uptime() / 60;
        float resetAtMinutes = NWScript.GetLocalFloat(NwModule.Instance, ResetTimerLVar);

        Log.Info($"Time since reset timer began: {(double)uptime / 60:0.00}");

        if (!(uptime >= resetAtMinutes)) return;

        _shutdownManager.InitiateShutdown();
    }
}