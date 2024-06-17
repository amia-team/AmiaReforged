using AmiaReforged.Settlements.Services.Economy.Initialization;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Settlements.Services.Economy;

[ServiceBinding(typeof(EconomySystemStartup))]
public class EconomySystemStartup
{
    private readonly IEnumerable<IResourceInitializer> _initializers;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();


    public EconomySystemStartup(IEnumerable<IResourceInitializer> initializers)
    {
        _initializers = initializers;

        Initialize();

        Log.Info("Economy System initialized.");
    }

    private async void Initialize()
    {
        foreach (IResourceInitializer initializer in _initializers)
        {
            await initializer.Initialize();
        }

        await NwTask.SwitchToMainThread();
    }
}