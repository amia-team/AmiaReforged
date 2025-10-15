using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine;

[ServiceBinding(typeof(ResourceWatcherService))]
public class ResourceWatcherService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly FileSystemWatcher _watcher = null!;
    public event EventHandler<FileSystemEventArgs>? FileSystemChanged;


    public ResourceWatcherService()
    {
        string resourcesPath = Environment.GetEnvironmentVariable("RESOURCE_PATH") ?? string.Empty;

        if (resourcesPath == string.Empty)
        {
            return;
        }

        _watcher = new FileSystemWatcher(resourcesPath)
        {
            IncludeSubdirectories = true,
            Filter = "*",
            NotifyFilter = NotifyFilters.LastWrite,
            EnableRaisingEvents = true,
            InternalBufferSize = 64 * 1024 // helps avoid dropped events on bursts
        };

        _watcher.Created += OnFsEvent;
        _watcher.Changed += OnFsEvent;
        _watcher.Deleted += OnFsEvent;
        _watcher.Renamed += OnFsEvent;
        _watcher.Error += OnFsError;
    }

    private void OnFsEvent(object sender, FileSystemEventArgs e)
    {
        FileSystemChanged?.Invoke(this, e);
    }

    private void OnFsError(object sender, ErrorEventArgs e)
    {
        Log.Error(e.GetException(), "File system watcher error occurred");
    }
}
