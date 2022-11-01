using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.System.Helpers;

public class NwTaskHelper
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public  async Task TrySwitchToMainThread()
    {
        try
        {
            await NwTask.SwitchToMainThread();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error switching to main thread");
        }
    }
}