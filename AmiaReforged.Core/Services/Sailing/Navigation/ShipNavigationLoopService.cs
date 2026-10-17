using Anvil.API;
using Anvil.Services;
using NLog;
namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(ShipNavigationLoopService))]
public sealed class ShipNavigationLoopService : IDisposable
{
    private readonly CancellationTokenSource _stop = new();
    private readonly HelmService _helm;
    private readonly ShipCombatNuiService _combat;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public Task Completion { get; private set; } = Task.CompletedTask;
    private bool _started;
    private int _disposed;
    public ShipNavigationLoopService(HelmService helmService, ShipCombatNuiService shipCombatNuiService)
    {
        _helm = helmService; _combat = shipCombatNuiService;
    }
    internal void Start(Task initialization)
    {
        if (_disposed != 0 || _started) return;
        _started = true;
        Completion = NavigationLoop(initialization, _stop.Token);
    }
    private async Task NavigationLoop(Task initialization, CancellationToken cancellation)
    {
        try { await initialization.WaitAsync(cancellation); }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { return; }
        catch (Exception ex) { Log.Error(ex, "Sailing navigation did not start because fleet initialization failed."); return; }
        while (!cancellation.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(1000, cancellation);
                await NwTask.SwitchToMainThread();
                if (cancellation.IsCancellationRequested) break;
                await _helm.NavigateAllShips();
                await NwTask.SwitchToMainThread();
                if (!cancellation.IsCancellationRequested) _combat.RefreshAll();
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { break; }
            catch (Exception ex) { Log.Error(ex, "Error in sailing tick."); }
        }
    }
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _stop.Cancel();
        if (!_started)
        {
            _stop.Dispose();
            return;
        }
        // Capture the final coherent fleet while shutdown still owns the main
        // thread. The persistence services flush these pure I/O tasks on disposal.
        try
        {
            Task<bool> checkpoint = _helm.CheckpointFleet();
            if (!checkpoint.Wait(TimeSpan.FromSeconds(10)))
                Log.Error("Timed out writing the final sailing fleet checkpoint.");
            else if (!checkpoint.Result) Log.Error("Some final sailing ship saves failed.");
        }
        catch (Exception ex) { Log.Error(ex, "Final sailing fleet checkpoint failed."); }
        _ = Completion.ContinueWith(_ => _stop.Dispose(), TaskScheduler.Default);
    }
}
