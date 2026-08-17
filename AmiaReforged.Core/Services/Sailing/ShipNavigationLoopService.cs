using Anvil.API;
using Anvil.Services;
using NLog;
namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(ShipNavigationLoopService))]
public class ShipNavigationLoopService
{
    private const int NavigationIntervalMilliseconds =
        1000;

    private readonly HelmService _helmService;

    private static readonly Logger Log =
        LogManager.GetCurrentClassLogger();

    public ShipNavigationLoopService(
        HelmService helmService)
    {
        _helmService =
            helmService;

        Log.Info(
            "Ship Navigation Loop Service initialized. " +
            $"Interval={NavigationIntervalMilliseconds}ms.");

        _ = NavigationLoop();
    }

    private async Task NavigationLoop()
    {
        while (true)
        {
            try
            {
                await Task.Delay(
                    NavigationIntervalMilliseconds);

                await NwTask.SwitchToMainThread();

               _helmService.NavigateAllShips();
            }
            catch (Exception ex)
            {
                Log.Error(
                    ex,
                    "Error in ship navigation loop.");
            }
        }
    }
}