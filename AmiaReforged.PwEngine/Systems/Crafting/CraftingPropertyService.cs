using AmiaReforged.PwEngine.Systems.Crafting.Models;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Systems.Crafting;

/// <summary>
/// This service is responsible for loading and providing item property definitions to the crafting system.
/// For example, it can provide a matrix of properties given an item type.
/// </summary>
[ServiceBinding(typeof(CraftingPropertyService))]
public class CraftingPropertyService
{
    private const string DirectoryEnvironmentVariable = "AMIA_CRAFTING_PROPERTIES_DIRECTORY";
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Do not use this constructor. It is only used for dependency injection. Inject into your class instead as a
    /// dependency (parameter) for its constructor to use it or utilize the [Inject] attribute if you need
    /// less strict control.
    /// </summary>
    public CraftingPropertyService()
    {
        LoadDefinitionsFromDisk();
    }

    private void LoadDefinitionsFromDisk()
    {
        // Log the user home directory's contents...
        string userHome = "/nwn/home"; // TODO: Do not hardcode this.
        Log.Info($"User home directory: {userHome}");
        foreach (string file in Directory.GetFiles(userHome))
        {
            Log.Info($"{file}");
        }

        string path =
            userHome + "/" + UtilPlugin.GetEnvironmentVariable(DirectoryEnvironmentVariable);
        if (path == "")
        {
            Log.Error(
                "Could not load crafting properties. Environment variable AMIA_CRAFTING_PROPERTIES_DIRECTORY is not set.");
        }
            
        // See if the path exists
        if (!Directory.Exists(path))
        {
            // Log absolute path.
            Log.Error($"Could not load crafting properties. Directory {path} does not exist.");
            return;
        }
        
        Log.Info("Loading crafting properties from disk...");
    }

    public IReadOnlyCollection<ItemPropertyCategory> GetPropertiesForItem(NwItem item)
    {
        return new List<ItemPropertyCategory>();
    }
}