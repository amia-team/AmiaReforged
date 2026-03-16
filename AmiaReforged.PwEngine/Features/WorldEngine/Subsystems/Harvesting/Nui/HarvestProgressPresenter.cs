using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Nui;

/// <summary>
/// Lightweight presenter for the harvest progress bar popup.
/// Strategies call <see cref="UpdateProgress"/> on each hit/tick and
/// <see cref="Complete"/> when the harvest cycle finishes, which auto-closes
/// the window after a brief delay.
/// </summary>
public sealed class HarvestProgressPresenter : ScryPresenter<HarvestProgressView>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly NwPlayer _player;
    private readonly string _title;

    private NuiWindowToken _token;
    private NuiWindow? _window;

    public HarvestProgressPresenter(HarvestProgressView view, NwPlayer player, string title)
    {
        View = view;
        _player = player;
        _title = title;
    }

    public override HarvestProgressView View { get; }

    public override NuiWindowToken Token() => _token;

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
    }

    /// <summary>
    /// Sets the progress bar to <paramref name="current"/> out of <paramref name="total"/>.
    /// </summary>
    public void UpdateProgress(int current, int total)
    {
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
            RaiseCloseEvent();
            Close();
        });
    }

    public override void Close()
    {
        try { _token.Close(); }
        catch { /* token may already be invalid */ }
    }
}
