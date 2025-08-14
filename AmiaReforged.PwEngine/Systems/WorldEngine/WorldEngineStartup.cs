using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Systems.WorldEngine;

// [ServiceBinding(typeof(WorldEngineStartup))]
public class WorldEngineStartup
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public WorldEngineStartup(ResourceWatcherService watcher, IWorldDataReloader reloader,
        IWorldEngineInitializer initializer)
    {
        watcher.FileSystemChanged += ReloadData;
    }

    private void ReloadData(object? sender, FileSystemEventArgs e)
    {
    }
}

public interface IWorldEngineInitializer
{
}

public interface IWorldDataReloader
{
    void ReloadData();
}
