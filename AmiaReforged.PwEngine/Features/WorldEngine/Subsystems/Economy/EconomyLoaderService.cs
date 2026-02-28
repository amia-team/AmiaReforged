using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy;

[ServiceBinding(typeof(EconomyLoaderService))]
public class EconomyLoaderService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly ItemBlueprintLoadingService _blueprintLoader;
    private readonly IndustryDefinitionLoadingService _industryLoader;
    private readonly RegionDefinitionLoadingService _regionLoader;
    private readonly NpcShopLoader _shopLoader;
    private readonly CoinhouseLoader _coinhouseLoader;

    public EconomyLoaderService(
        ItemBlueprintLoadingService blueprintLoader,
        IndustryDefinitionLoadingService industryLoader,
        RegionDefinitionLoadingService regionLoader,
        NpcShopLoader shopLoader,
        CoinhouseLoader coinhouseLoader,
        ResourceWatcherService resourceWatcherService)
    {
        string resourcePath = UtilPlugin.GetEnvironmentVariable("RESOURCE_PATH");

        Environment.SetEnvironmentVariable("RESOURCE_PATH", resourcePath);

        _blueprintLoader = blueprintLoader;
        _industryLoader = industryLoader;
        _regionLoader = regionLoader;
        _shopLoader = shopLoader;
        _coinhouseLoader = coinhouseLoader;

        resourceWatcherService.FileSystemChanged += ReloadChanges;
    }

    private void ReloadChanges(object? sender, FileSystemEventArgs e)
    {
        if(e.Name is null) return;

        if(!e.Name.EndsWith(".json")) return;
        Log.Info($"Reloading {e.Name}");
        LoadDefinitions();
    }

    public void Startup()
    {
        LoadDefinitions();
    }

    private void LoadDefinitions()
    {
        Log.Info("=== Starting WorldEngine Economy Definition Loading ===");
        DateTime startTime = DateTime.UtcNow;

        Log.Info("Loading Industries...");
        _industryLoader.Load();
        LogLoadResults("Industries", _industryLoader.Failures());

        // Item definitions and resource node definitions are now loaded from the database.
        // They are managed via the WorldEngine admin panel and persisted in PostgreSQL.
        Log.Info("Item definitions and resource nodes are now database-backed (skipping JSON loading)");

        Log.Info("Loading Item Blueprints...");
        _blueprintLoader.Load();
        LogLoadResults("Item Blueprints", _blueprintLoader.Failures());

        Log.Info("Loading Regions...");
        _regionLoader.Load();
        LogLoadResults("Regions", _regionLoader.Failures());

        Log.Info("Loading NPC Shops...");
        _shopLoader.Load();
        LogLoadResults("NPC Shops", _shopLoader.Failures());

        Log.Info("Loading Coinhouses...");
        _coinhouseLoader.Load();
        LogLoadResults("Coinhouses", _coinhouseLoader.Failures());

        TimeSpan elapsed = DateTime.UtcNow - startTime;
        Log.Info($"=== Economy Definition Loading Complete in {elapsed.TotalMilliseconds:F0}ms ===");
    }

    private static void LogLoadResults(string loaderName, List<FileLoadResult> failures)
    {
        if (failures.Count == 0)
        {
            Log.Info($"  ✓ {loaderName} loaded successfully");
            return;
        }

        int failureCount = failures.Count(f => f.Type == ResultType.Fail);
        int warningCount = failures.Count(f => f.Type == ResultType.Warning);

        if (failureCount > 0)
        {
            Log.Error($"  ✗ {loaderName} had {failureCount} failure(s){(warningCount > 0 ? $" and {warningCount} warning(s)" : "")}:");
        }
        else if (warningCount > 0)
        {
            Log.Warn($"  ⚠ {loaderName} loaded with {warningCount} warning(s):");
        }

        foreach (FileLoadResult failure in failures)
        {
            if (failure.Type == ResultType.Warning)
            {
                Log.Warn($"    • File: {failure.FileName}");
                Log.Warn($"      Warning: {failure.Message}");
                continue;
            }

            Log.Error($"    • File: {failure.FileName}");
            Log.Error($"      Error: {failure.Message}");

            if (failure.Type == ResultType.Fail)
            {
                Log.Error($"      Type: FAILURE - This file was not loaded");
            }

            if (failure.Exception != null)
            {
                Log.Error($"      Exception Type: {failure.Exception.GetType().FullName}");
                Log.Error($"      Stack Trace:");

                // Log each line of the stack trace with proper indentation
                string? stackTrace = failure.Exception.StackTrace;
                if (!string.IsNullOrEmpty(stackTrace))
                {
                    string[] lines = stackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string line in lines)
                    {
                        Log.Error($"        {line.Trim()}");
                    }
                }

                // Log inner exceptions if present
                Exception? innerEx = failure.Exception.InnerException;
                int depth = 1;
                while (innerEx != null && depth <= 3) // Limit to 3 levels deep
                {
                    Log.Error($"      Inner Exception [{depth}]: {innerEx.GetType().FullName}");
                    Log.Error($"        Message: {innerEx.Message}");
                    if (!string.IsNullOrEmpty(innerEx.StackTrace))
                    {
                        string[] innerLines = innerEx.StackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string line in innerLines.Take(5)) // First 5 lines only
                        {
                            Log.Error($"          {line.Trim()}");
                        }
                    }
                    innerEx = innerEx.InnerException;
                    depth++;
                }
            }
        }
    }
}
