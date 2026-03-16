using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Nui;

/// <summary>
/// Lightweight presenter for the harvest progress bar popup.
/// Strategies call <see cref="UpdateProgress"/> on each hit/tick and
/// <see cref="Complete"/> when the harvest cycle finishes, which auto-closes
/// the window after a brief delay.
/// <para>
/// Implements <see cref="IAutoCloseOnMove"/> so the window closes if the player
/// walks away from the node. Also runs an inactivity timer — if no progress
/// update arrives within <see cref="InactivityTimeout"/>, the window auto-closes
/// (covers the case where a player stops attacking).
/// </para>
/// </summary>
public sealed class HarvestProgressPresenter : ScryPresenter<HarvestProgressView>, IAutoCloseOnMove
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// How long to wait without a progress update before auto-closing.
    /// NWN combat rounds are 6 seconds; 8 gives a comfortable buffer.
    /// </summary>
    private static readonly TimeSpan InactivityTimeout = TimeSpan.FromSeconds(8);

    private readonly NwPlayer _player;
    private readonly string _title;

    private NuiWindowToken _token;
    private NuiWindow? _window;
    private DateTime _lastUpdateUtc;
    private bool _closed;

    /// <summary>
    /// Fired when the window is closed for any reason (move, inactivity, completion).
    /// Strategies should subscribe to clean up their per-player tracking dictionaries.
    /// </summary>
    public event System.Action? OnClosed;

    public HarvestProgressPresenter(HarvestProgressView view, NwPlayer player, string title)
    {
        View = view;
        _player = player;
        _title = title;
    }

    public override HarvestProgressView View { get; }

    public override NuiWindowToken Token() => _token;

    // IAutoCloseOnMove — poll every second, close if player moves > 0.5m
    public TimeSpan AutoClosePollInterval => TimeSpan.FromSeconds(1);
    public float AutoCloseMovementThreshold => 0.5f;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), _title)
        {
            Geometry = new NuiRect(-1f, -1f, HarvestProgressView.WindowW, HarvestProgressView.WindowH),
            Closable = false,
            Resizable = false,
            Collapsed = false,
            Border = true
        };
    }

    public override void Create()
    {
        if (_window == null)
        {
            Log.Warn("HarvestProgressPresenter.Create called before InitBefore");
            return;
        }

        if (!_player.TryCreateNuiWindow(_window, out _token))
        {
            Log.Warn("Unable to open harvest progress window for {Player}", _player.PlayerName);
            return;
        }

        _token.SetBindValue(View.ProgressValue, 0f);
        _lastUpdateUtc = DateTime.UtcNow;
        _closed = false;

        // Start inactivity watchdog
        StartInactivityWatchdog();
    }

    /// <summary>
    /// Sets the progress bar to <paramref name="current"/> out of <paramref name="total"/>.
    /// Resets the inactivity timer.
    /// </summary>
    public void UpdateProgress(int current, int total)
    {
        if (_closed) return;

        _lastUpdateUtc = DateTime.UtcNow;

        if (total <= 0) total = 1;
        float value = Math.Clamp((float)current / total, 0f, 1f);
        try
        {
            _token.SetBindValue(View.ProgressValue, value);
        }
        catch
        {
            // Token may have been closed externally
        }
    }

    /// <summary>
    /// Fills the bar to 100 % and closes the window after a short delay.
    /// </summary>
    public void Complete()
    {
        if (_closed) return;

        try
        {
            _token.SetBindValue(View.ProgressValue, 1f);
        }
        catch
        {
            // Token may have been closed externally
        }

        _ = NwTask.Run(async () =>
        {
            await NwTask.Delay(TimeSpan.FromMilliseconds(600));
            await NwTask.SwitchToMainThread();
            CloseInternal();
        });
    }

    public override void Close()
    {
        CloseInternal();
    }

    private void CloseInternal()
    {
        if (_closed) return;
        _closed = true;

        try { _token.Close(); }
        catch { /* token may already be invalid */ }

        RaiseCloseEvent();
        OnClosed?.Invoke();
    }

    private void StartInactivityWatchdog()
    {
        _ = NwTask.Run(async () =>
        {
            while (!_closed)
            {
                await NwTask.Delay(TimeSpan.FromSeconds(2));
                await NwTask.SwitchToMainThread();

                if (_closed) break;

                if (DateTime.UtcNow - _lastUpdateUtc > InactivityTimeout)
                {
                    CloseInternal();
                }
            }
        });
    }
}
