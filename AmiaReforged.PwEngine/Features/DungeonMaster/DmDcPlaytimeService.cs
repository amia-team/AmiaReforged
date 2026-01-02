using AmiaReforged.Core.Services;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.DungeonMaster;

/// <summary>
///   Service that tracks DM playtime and awards Dreamcoins.
///   Awards 2 DC per 2 hours (120 minutes) of playtime.
///   Uses a single scheduler tick and in-memory tracking per session.
///   Only runs on live server.
/// </summary>
[ServiceBinding(typeof(DmDcPlaytimeService))]
public sealed class DmDcPlaytimeService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private const int MinutesPerDc = 120;
    private const int DcPerAward = 2;
    private const int TickIntervalMinutes = 5;

    private readonly DreamcoinService _dreamcoinService;
    private readonly SchedulerService _schedulerService;
    private readonly bool _isLiveServer;

    /// <summary>
    ///   In-memory tracking of accumulated playtime per DM CD key.
    ///   Resets on server restart - playtime only counts per session.
    /// </summary>
    private readonly Dictionary<string, int> _accumulatedMinutes = new();

    public DmDcPlaytimeService(DreamcoinService dreamcoinService, SchedulerService schedulerService)
    {
        _dreamcoinService = dreamcoinService;
        _schedulerService = schedulerService;

        // Check if we're on live server
        string environment = UtilPlugin.GetEnvironmentVariable("SERVER_MODE");
        _isLiveServer = environment == "live";

        if (!_isLiveServer)
        {
            Log.Info("DmDcPlaytimeService: Not on live server, DM DC awards disabled.");
            return;
        }

        // Register a single repeating task for all DMs
        _schedulerService.ScheduleRepeating(OnPlaytimeTick, TimeSpan.FromMinutes(TickIntervalMinutes));

        Log.Info("DmDcPlaytimeService initialized. Tracking DM playtime for DC awards (2 DC per 2 hours).");
    }

    private async void OnPlaytimeTick()
    {
        // Get all online DMs
        List<NwPlayer> onlineDms = NwModule.Instance.Players
            .Where(p => p.IsValid && p.IsDM)
            .ToList();

        foreach (NwPlayer dm in onlineDms)
        {
            string cdKey = dm.CDKey;

            // Initialize if not tracked yet
            if (!_accumulatedMinutes.ContainsKey(cdKey))
            {
                _accumulatedMinutes[cdKey] = 0;
            }

            // Increment playtime
            _accumulatedMinutes[cdKey] += TickIntervalMinutes;

            // Check if DC should be awarded
            if (_accumulatedMinutes[cdKey] >= MinutesPerDc)
            {
                _accumulatedMinutes[cdKey] = 0;

                int newBalance = await _dreamcoinService.AddDreamcoins(cdKey, DcPerAward);
                await NwTask.SwitchToMainThread();

                if (newBalance >= 0)
                {
                    dm.SendServerMessage($"You have been awarded {DcPerAward} Dreamcoins for 2 hours of DM time!", ColorConstants.Yellow);
                    Log.Info($"Awarded {DcPerAward} DC to DM {dm.PlayerName} ({cdKey}) for playtime. New balance: {newBalance}");
                }
                else
                {
                    Log.Warn($"Failed to award DC to DM {dm.PlayerName} ({cdKey})");
                }
            }
        }

        // Clean up disconnected DMs from dictionary
        List<string> onlineCdKeys = onlineDms.Select(p => p.CDKey).ToList();
        List<string> disconnectedKeys = _accumulatedMinutes.Keys
            .Where(k => !onlineCdKeys.Contains(k))
            .ToList();

        foreach (string key in disconnectedKeys)
        {
            _accumulatedMinutes.Remove(key);
        }
    }
}
