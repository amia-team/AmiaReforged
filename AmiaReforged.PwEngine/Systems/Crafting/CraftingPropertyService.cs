using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Systems.Crafting;

[ServiceBinding(typeof(CraftingPropertyService))]
public class CraftingPropertyService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public CraftingPropertyService()
    {
        LoadDefinitionsFromDisk();
    }

    private void LoadDefinitionsFromDisk()
    {
        
    }
}