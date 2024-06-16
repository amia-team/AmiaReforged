using Anvil.Services;

namespace AmiaReforged.Settlements.Services.ResourceManagement;

public static class StockpileConstants
{
    public const string StockpileTag = "STOCKPILE";
    public const string StockpileNameLocalVariable = "stockpile_name";
    public const string StockpileIdLocalVariable = "stockpile_id";
    public const string SettlementIdLocalVariable = "settlement_id";

    [Inject] private static PluginStorageService? PluginStorageService { get; set; }

    public static Uri EconomyItemFiles
    {
        get
        {
            string absolutePath = "";
            try
            {
                absolutePath = PluginStorageService!.GetPluginStoragePath(typeof(StockpileConstants).Assembly) + "/Items/";
            }
            catch
            {
                absolutePath = Path.GetFullPath("Anvil/PluginData/AmiaReforged.Settlements/Items/");
            }

            return new Uri($"file://{absolutePath}");
        }
    }

    public static Uri MaterialFiles
    {
        get
        {
            // string absolutePath = Path.GetFullPath(HomeStorage.PluginData + "/AmiaReforged.Settlements/Materials/");
            string absolutePath = "";
            try
            {
                absolutePath = PluginStorageService!.GetPluginStoragePath(typeof(StockpileConstants).Assembly) + "/Materials/";
            }
            catch
            {
                absolutePath = Path.GetFullPath("Anvil/PluginData/AmiaReforged.Settlements/Materials/");
            }
            return new Uri($"file://{absolutePath}");
        }
    }

    public static Uri QualityFiles
    {
        get
        {
            // string absolutePath = Path.GetFullPath(HomeStorage.PluginData + "/AmiaReforged.Settlements/ItemQuality/");
            string absolutePath = "";
            try
            {
                absolutePath = PluginStorageService!.GetPluginStoragePath(typeof(StockpileConstants).Assembly) + "/ItemQuality/";
            }
            catch
            {
                absolutePath = Path.GetFullPath("Anvil/PluginData/AmiaReforged.Settlements/ItemQuality/");
            }
            return new Uri($"file://{absolutePath}");
        }
    }
}