using AmiaReforged.Core.Services;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.DungeonMaster;

/// <summary>
///   Service that tracks DM playtime and awards Dreamcoins.
///   Awards 2 DC per 2 hours (120 minutes) of playtime.
///   Uses persistent database storage for weekly playtime tracking.
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
    private readonly DmPlaytimeService _playtimeService;
    private readonly SchedulerService _schedulerService;
    private readonly bool _isLiveServer;

    /// <summary>
    ///   In-memory cache of accumulated minutes for quick access during ticks.
    ///   Synchronized with database on each tick.
    /// </summary>
    private readonly Dictionary<string, int> _cachedMinutes = new();

    public DmDcPlaytimeService(DreamcoinService dreamcoinService, DmPlaytimeService playtimeService, SchedulerService schedulerService)
    {
        _dreamcoinService = dreamcoinService;
        _playtimeService = playtimeService;
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

        Log.Info("DmDcPlaytimeService initialized. Tracking DM playtime for DC awards (2 DC per 2 hours) with persistent storage.");
    }

    /// <summary>
    ///   Gets the remaining minutes until the next DC award for a DM.
    /// </summary>
    /// <param name="cdKey">The DM's CD key.</param>
    /// <returns>Minutes remaining, or the full MinutesPerDc if not tracked yet.</returns>
    public int GetMinutesUntilNextDc(string cdKey)
    {
        if (_cachedMinutes.TryGetValue(cdKey, out int accumulated))
        {
            return Math.Max(0, MinutesPerDc - accumulated);
        }
        return MinutesPerDc;
    }

    /// <summary>
    ///   Gets the remaining minutes until the next DC award for a DM (async version that queries DB).
    /// </summary>
    /// <param name="cdKey">The DM's CD key.</param>
    /// <returns>Minutes remaining until next DC.</returns>
    public async Task<int> GetMinutesUntilNextDcAsync(string cdKey)
    {
        int accumulated = await _playtimeService.GetMinutesTowardNextDc(cdKey);
        return Math.Max(0, MinutesPerDc - accumulated);
    }

    /// <summary>
    ///   Gets the total minutes played this week as DM.
    /// </summary>
    /// <param name="cdKey">The DM's CD key.</param>
    /// <returns>Total minutes played this week as DM.</returns>
    public async Task<int> GetWeeklyMinutesPlayed(string cdKey)
    {
        return await _playtimeService.GetWeeklyMinutesPlayed(cdKey);
    }

    /// <summary>
    ///   Gets the total all-time minutes played as DM.
    /// </summary>
    /// <param name="cdKey">The DM's CD key.</param>
    /// <returns>Total minutes played as DM across all time.</returns>
    public async Task<int> GetTotalMinutesPlayed(string cdKey)
    {
        return await _playtimeService.GetTotalMinutesPlayed(cdKey);
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

            try
            {
                // Add playtime to persistent storage and get updated record
                var record = await _playtimeService.AddPlaytimeMinutes(cdKey, TickIntervalMinutes);
                await NwTask.SwitchToMainThread();

                // Update cache
                _cachedMinutes[cdKey] = record.MinutesTowardNextDc;

                // Check if DC should be awarded
                if (record.MinutesTowardNextDc >= MinutesPerDc)
                {
                    await _playtimeService.ResetMinutesTowardNextDc(cdKey, MinutesPerDc);
                    await NwTask.SwitchToMainThread();

                    // Update cache after reset
                    _cachedMinutes[cdKey] = Math.Max(0, record.MinutesTowardNextDc - MinutesPerDc);

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
            catch (Exception ex)
            {
                Log.Error($"Error processing DM playtime tick for {dm.PlayerName} ({cdKey}): {ex.Message}");
            }
        }

        // Clean up disconnected DMs from cache
        List<string> onlineCdKeys = onlineDms.Select(p => p.CDKey).ToList();
        List<string> disconnectedKeys = _cachedMinutes.Keys
            .Where(k => !onlineCdKeys.Contains(k))
            .ToList();

        foreach (string key in disconnectedKeys)
        {
            _cachedMinutes.Remove(key);
        }
    }
}
