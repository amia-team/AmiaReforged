using AmiaReforged.Core.Services;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.Player.DreamcoinTool;

/// <summary>
///   Service that tracks player playtime and awards Dreamcoins.
///   Awards 1 DC per 2 hours (120 minutes) of playtime.
///   Uses a single scheduler tick and in-memory tracking per session.
/// </summary>
[ServiceBinding(typeof(DcPlaytimeService))]
public sealed class DcPlaytimeService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private const int MinutesPerDc = 120;
    private const int TickIntervalMinutes = 5;

    private readonly DreamcoinService _dreamcoinService;
    private readonly SchedulerService _schedulerService;

    /// <summary>
    ///   In-memory tracking of accumulated playtime per CD key.
    ///   Resets on server restart - playtime only counts per session.
    /// </summary>
    private readonly Dictionary<string, int> _accumulatedMinutes = new();

    public DcPlaytimeService(DreamcoinService dreamcoinService, SchedulerService schedulerService)
    {
        _dreamcoinService = dreamcoinService;
        _schedulerService = schedulerService;

        // Register a single repeating task for all players
        _schedulerService.ScheduleRepeating(OnPlaytimeTick, TimeSpan.FromMinutes(TickIntervalMinutes));

        Log.Info("DcPlaytimeService initialized. Tracking player playtime for DC awards.");
    }

    /// <summary>
    ///   Gets the remaining minutes until the next DC award for a player.
    /// </summary>
    /// <param name="cdKey">The player's CD key.</param>
    /// <returns>Minutes remaining, or the full MinutesPerDc if not tracked yet.</returns>
    public int GetMinutesUntilNextDc(string cdKey)
    {
        if (_accumulatedMinutes.TryGetValue(cdKey, out int accumulated))
        {
            return MinutesPerDc - accumulated;
        }
        return MinutesPerDc;
    }

    private async void OnPlaytimeTick()
    {
        // Get all online players (excluding DMs)
        List<NwPlayer> onlinePlayers = NwModule.Instance.Players
            .Where(p => p.IsValid && !p.IsDM)
            .ToList();

        foreach (NwPlayer player in onlinePlayers)
        {
            string cdKey = player.CDKey;

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

                int newBalance = await _dreamcoinService.AddDreamcoins(cdKey, 1);
                await NwTask.SwitchToMainThread();

                if (newBalance >= 0)
                {
                    player.SendServerMessage("You have been awarded 1 Dreamcoin for 2 hours of playtime!", ColorConstants.Yellow);
                    Log.Info($"Awarded 1 DC to {player.PlayerName} ({cdKey}) for playtime. New balance: {newBalance}");
                }
                else
                {
                    Log.Warn($"Failed to award DC to {player.PlayerName} ({cdKey})");
                }
            }
        }

        // Clean up disconnected players from dictionary
        List<string> onlineCdKeys = onlinePlayers.Select(p => p.CDKey).ToList();
        List<string> disconnectedKeys = _accumulatedMinutes.Keys
            .Where(k => !onlineCdKeys.Contains(k))
            .ToList();

        foreach (string key in disconnectedKeys)
        {
            _accumulatedMinutes.Remove(key);
        }
    }
}
