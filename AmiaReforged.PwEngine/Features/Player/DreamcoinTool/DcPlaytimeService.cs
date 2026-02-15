using AmiaReforged.Core.Services;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.Player.DreamcoinTool;

/// <summary>
///   Service that tracks player playtime and awards Dreamcoins.
///   Awards 1 DC per 2 hours (120 minutes) of playtime.
///   Uses persistent database storage for weekly playtime tracking.
/// </summary>
[ServiceBinding(typeof(DcPlaytimeService))]
public sealed class DcPlaytimeService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private const int MinutesPerDc = 120;
    private const int TickIntervalMinutes = 5;

    private readonly DreamcoinService _dreamcoinService;
    private readonly PlayerPlaytimeService _playtimeService;
    private readonly SchedulerService _schedulerService;

    /// <summary>
    ///   In-memory cache of accumulated minutes for quick access during ticks.
    ///   Synchronized with database on each tick.
    /// </summary>
    private readonly Dictionary<string, int> _cachedMinutes = new();

    public DcPlaytimeService(DreamcoinService dreamcoinService, PlayerPlaytimeService playtimeService, SchedulerService schedulerService)
    {
        _dreamcoinService = dreamcoinService;
        _playtimeService = playtimeService;
        _schedulerService = schedulerService;

        // Register a single repeating task for all players
        _schedulerService.ScheduleRepeating(OnPlaytimeTick, TimeSpan.FromMinutes(TickIntervalMinutes));

        Log.Info("DcPlaytimeService initialized. Tracking player playtime for DC awards with persistent storage.");
    }

    /// <summary>
    ///   Gets the remaining minutes until the next DC award for a player.
    /// </summary>
    /// <param name="cdKey">The player's CD key.</param>
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
    ///   Gets the remaining minutes until the next DC award for a player (async version that queries DB).
    /// </summary>
    /// <param name="cdKey">The player's CD key.</param>
    /// <returns>Minutes remaining until next DC.</returns>
    public async Task<int> GetMinutesUntilNextDcAsync(string cdKey)
    {
        int accumulated = await _playtimeService.GetMinutesTowardNextDc(cdKey);
        return Math.Max(0, MinutesPerDc - accumulated);
    }

    /// <summary>
    ///   Gets the total minutes played this week for a player.
    /// </summary>
    /// <param name="cdKey">The player's CD key.</param>
    /// <returns>Total minutes played this week.</returns>
    public async Task<int> GetWeeklyMinutesPlayed(string cdKey)
    {
        return await _playtimeService.GetWeeklyMinutesPlayed(cdKey);
    }

    /// <summary>
    ///   Gets the total all-time minutes played for a player.
    /// </summary>
    /// <param name="cdKey">The player's CD key.</param>
    /// <returns>Total minutes played across all time.</returns>
    public async Task<int> GetTotalMinutesPlayed(string cdKey)
    {
        return await _playtimeService.GetTotalMinutesPlayed(cdKey);
    }

    private async void OnPlaytimeTick()
    {
        try
        {
        // Capture all player data while on main thread
        List<(NwPlayer player, string cdKey, string playerName)> playerData = NwModule.Instance.Players
            .Where(p => p.IsValid && !p.IsDM)
            .Select(p => (player: p, cdKey: p.CDKey, playerName: p.PlayerName))
            .ToList();

        foreach (var (player, cdKey, playerName) in playerData)
        {
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

                    int newBalance = await _dreamcoinService.AddDreamcoins(cdKey, 1);
                    await NwTask.SwitchToMainThread();

                    if (newBalance >= 0 && player.IsValid)
                    {
                        player.SendServerMessage("You have been awarded 1 Dreamcoin for 2 hours of playtime!", ColorConstants.Yellow);
                        Log.Info($"Awarded 1 DC to {playerName} ({cdKey}) for playtime. New balance: {newBalance}");
                    }
                    else if (newBalance < 0)
                    {
                        Log.Warn($"Failed to award DC to {playerName} ({cdKey})");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error processing playtime tick for {playerName} ({cdKey}): {ex.Message}");
                await NwTask.SwitchToMainThread();
            }
        }

        // Clean up disconnected players from cache
        HashSet<string> onlineCdKeys = playerData.Select(p => p.cdKey).ToHashSet();
        List<string> disconnectedKeys = _cachedMinutes.Keys
            .Where(k => !onlineCdKeys.Contains(k))
            .ToList();

        foreach (string key in disconnectedKeys)
        {
            _cachedMinutes.Remove(key);
        }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in DcPlaytimeService.OnPlaytimeTick");
        }
    }
}
