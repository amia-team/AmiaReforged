using AmiaReforged.PwEngine.Systems.Crafting.Models;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Systems.Crafting;

/// <summary>
/// This service is responsible for loading and providing item property definitions to the crafting system.
/// For example, it can provide a matrix of properties given an item type.
/// </summary>
[ServiceBinding(typeof(CraftingPropertyService))]
public class CraftingPropertyService
{
    private const string AmiaCraftingPropertiesDirectory = "AMIA_CRAFTING_PROPERTIES_DIRECTORY";
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public CraftingPropertyService()
    {
        LoadDefinitionsFromDisk();
    }

    private void LoadDefinitionsFromDisk()
    {
        string path = UtilPlugin.GetEnvironmentVariable(AmiaCraftingPropertiesDirectory);
        if (path == "")
        {
            Log.Error("Could not load crafting properties. Environment variable AMIA_CRAFTING_PROPERTIES_DIRECTORY is not set.");
        }
        
        // See if the path exists
        if (!Directory.Exists(path))
        {
            // Log absolute path.
            Log.Error($"Could not load crafting properties. Directory {path} does not exist.");
            return;
        }
    }
    
    public IReadOnlyCollection<ItemPropertyCategory> GetPropertiesForItem(NwItem item)
    {
        return new List<ItemPropertyCategory>();
    }
}