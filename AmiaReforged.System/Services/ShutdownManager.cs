using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.System.Services;

public class ShutdownManager
{
    private readonly SchedulerService _schedulerService;

    public ShutdownManager(SchedulerService schedulerService)
    {
        _schedulerService = schedulerService;
    }

    /// <summary>
    /// Saves and boots all PCs, then schedules the server to be shutdown shortly after.
    /// </summary>
    public void InitiateShutdown()
    {
        NwModule.Instance.Players.ToList().ForEach(p =>
        {
            p.SendServerMessage(
                "-- Amia is shutting down now. Please do not try to log in until it is done or you will be booted. --");
            p.ExportCharacter();
            p.BootPlayer("Server reset.");
        });

        _schedulerService.Schedule(NwServer.Instance.ShutdownServer, TimeSpan.FromSeconds(10));
    }
}