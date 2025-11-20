using System.Security.Cryptography;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

[ServiceBinding(typeof(ResourceWatcherService))]
public class ResourceWatcherService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly FileSystemWatcher _watcher = null!;
    private readonly Dictionary<string, string> _fileHashes = new();
    private readonly Timer? _debounceTimer;
    private readonly object _debounceLock = new();
    private readonly HashSet<string> _pendingChanges = new();
    private const int DebounceDelayMs = 500;

    public event EventHandler<FileSystemEventArgs>? FileSystemChanged;


    public ResourceWatcherService()
    {
        string resourcesPath = Environment.GetEnvironmentVariable("RESOURCE_PATH") ?? string.Empty;

        if (resourcesPath == string.Empty)
        {
            return;
        }

        // Initialize hash cache for all existing .json files
        InitializeFileHashes(resourcesPath);

        _watcher = new FileSystemWatcher(resourcesPath)
        {
            IncludeSubdirectories = true,
            Filter = "*.json",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            EnableRaisingEvents = true,
            InternalBufferSize = 64 * 1024 // helps avoid dropped events on bursts
        };

        _watcher.Created += OnFsEvent;
        _watcher.Changed += OnFsEvent;
        _watcher.Deleted += OnFsEvent;
        _watcher.Renamed += OnFsEvent;
        _watcher.Error += OnFsError;

        // Initialize debounce timer (but don't start it yet)
        _debounceTimer = new Timer(OnDebounceElapsed, null, Timeout.Infinite, Timeout.Infinite);

        Log.Info($"ResourceWatcherService initialized with {_fileHashes.Count} files cached");
    }

    private void InitializeFileHashes(string rootPath)
    {
        try
        {
            if (!Directory.Exists(rootPath))
            {
                Log.Warn($"Resource path does not exist: {rootPath}");
                return;
            }

            string[] jsonFiles = Directory.GetFiles(rootPath, "*.json", SearchOption.AllDirectories);

            foreach (string filePath in jsonFiles)
            {
                try
                {
                    string hash = ComputeFileHash(filePath);
                    _fileHashes[filePath] = hash;
                }
                catch (Exception ex)
                {
                    Log.Warn(ex, $"Failed to hash file during initialization: {filePath}");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize file hash cache");
        }
    }

    private string ComputeFileHash(string filePath)
    {
        using FileStream stream = File.OpenRead(filePath);
        using SHA256 sha256 = SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(stream);
        return Convert.ToHexString(hashBytes);
    }

    private void OnFsEvent(object sender, FileSystemEventArgs e)
    {
        if (e.Name is null || !e.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string fullPath = e.FullPath;

        // Handle deleted files - queue for processing but don't modify hashes yet
        if (e.ChangeType == WatcherChangeTypes.Deleted)
        {
            lock (_debounceLock)
            {
                _pendingChanges.Add(fullPath);
                ResetDebounceTimer();
            }
            Log.Debug($"File deleted: {e.Name}");
            return;
        }

        // Handle renamed files
        if (e is RenamedEventArgs renamedArgs)
        {
            lock (_debounceLock)
            {
                _fileHashes.Remove(renamedArgs.OldFullPath);
                // The new file will be handled as a Created/Changed event
            }
        }

        // For Created/Changed events, check if content actually changed
        // Don't block the FileSystemWatcher thread - just queue the change
        lock (_debounceLock)
        {
            _pendingChanges.Add(fullPath);
            ResetDebounceTimer();
        }
    }

    private void ResetDebounceTimer()
    {
        // Reset the timer to fire after the debounce delay
        _debounceTimer?.Change(DebounceDelayMs, Timeout.Infinite);
    }

    private void OnDebounceElapsed(object? state)
    {
        List<string> pendingFiles;

        lock (_debounceLock)
        {
            if (_pendingChanges.Count == 0)
            {
                return;
            }

            pendingFiles = new List<string>(_pendingChanges);
            _pendingChanges.Clear();
        }

        // Check which files actually changed content (outside the lock)
        List<string> actuallyChangedFiles = new();

        foreach (string filePath in pendingFiles)
        {
            try
            {
                // Check if file was deleted
                if (!File.Exists(filePath))
                {
                    bool wasTracked;
                    lock (_debounceLock)
                    {
                        wasTracked = _fileHashes.Remove(filePath);
                    }
                    // Report deletion if file was tracked OR if it's in our watch directory
                    // (FileSystemWatcher only fires events for files it's watching)
                    actuallyChangedFiles.Add(filePath);
                    Log.Debug($"File deleted: {Path.GetFileName(filePath)} (was tracked: {wasTracked})");
                    continue;
                }

                // Compute hash for existing files
                string newHash = ComputeFileHash(filePath);
                bool hasChanged;

                lock (_debounceLock)
                {
                    hasChanged = !_fileHashes.TryGetValue(filePath, out string? oldHash) || oldHash != newHash;
                    if (hasChanged)
                    {
                        _fileHashes[filePath] = newHash;
                    }
                }

                if (hasChanged)
                {
                    actuallyChangedFiles.Add(filePath);
                    Log.Debug($"File content changed: {Path.GetFileName(filePath)} (Hash: {newHash[..8]}...)");
                }
                else
                {
                    Log.Debug($"File modified but content unchanged: {Path.GetFileName(filePath)}");
                }
            }
            catch (IOException ioEx)
            {
                Log.Debug(ioEx, $"IO error reading file (may still be writing): {Path.GetFileName(filePath)}");
                // File might still be being written, reschedule
                lock (_debounceLock)
                {
                    _pendingChanges.Add(filePath);
                    ResetDebounceTimer();
                }
            }
            catch (Exception ex)
            {
                Log.Warn(ex, $"Failed to process file change: {Path.GetFileName(filePath)}");
            }
        }

        if (actuallyChangedFiles.Count == 0)
        {
            Log.Debug("No actual content changes detected after debounce");
            return;
        }

        Log.Info($"Debounced file changes detected: {actuallyChangedFiles.Count} file(s) changed");

        // Fire a single consolidated event
        // Use the first file path as representative for the event args
        string firstFile = actuallyChangedFiles[0];
        FileSystemEventArgs args = new FileSystemEventArgs(WatcherChangeTypes.Changed,
            Path.GetDirectoryName(firstFile) ?? string.Empty,
            Path.GetFileName(firstFile));

        FileSystemChanged?.Invoke(this, args);
    }

    private void OnFsError(object sender, ErrorEventArgs e)
    {
        Log.Error(e.GetException(), "File system watcher error occurred");
    }
}
