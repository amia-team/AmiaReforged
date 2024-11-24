using System.Globalization;
using AmiaReforged.PwEngine.Systems.Crafting.Models;
using Anvil.API;
using Anvil.Services;
using CsvHelper;
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

    private List<PropertyDefinition> _definitions = new();

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

        // It's assumed to be a CSV file.
        foreach (string file in Directory.GetFiles(path, "*.csv"))
        {
            Log.Info($"Loading crafting properties from {file}...");
            LoadDefinitionsFromFile(file);
        }
    }

    private void LoadDefinitionsFromFile(string file)
    {
        using StreamReader reader = new StreamReader(file);
        using CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        // Read the header
        if (!csv.Read() || !csv.ReadHeader())
        {
            Log.Error($"Failed to read header from {file}");
            return;
        }

        // Validate the header
        string[] expectedHeaders = new[] { "Property", "Category", "BaseItem", "Whitelist", "MythalPowers" };
        foreach (string header in expectedHeaders)
        {
            if (csv.HeaderRecord != null && csv.HeaderRecord.Contains(header)) continue;
            Log.Error($"Header validation failed for {file}. Missing column: {header}");
            return;
        }

        // Read the file line by line
        while (csv.Read())
        {
            string? property = csv.GetField<string>("Property");
            string? category = csv.GetField<string>("Category");
            int baseItem = csv.GetField<int>("BaseItem");
            string? whitelist = csv.GetField<string>("Whitelist");
            string? mythalPowers = csv.GetField<string>("MythalPowers");

            // Process the data as needed
            Dictionary<string, ItemProperty>
                properties = new Dictionary<string, ItemProperty>(); // Populate this dictionary as needed
            List<int> supportedItemTypes = new List<int> { baseItem }; // Populate this list as needed

            PropertyDefinition definition = new PropertyDefinition(category, properties, false)
            {
                SupportedItemTypes = supportedItemTypes
            };

            _definitions.Add(definition);
        }
    }

    public IReadOnlyCollection<PropertyDefinition> GetPropertiesForItem(NwItem item)
    {
        List<PropertyDefinition> result = new();

        // Don't use LINQ here, it's less readable.
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach (PropertyDefinition def in _definitions)
        {
            if (def.SupportedItemTypes.Contains(NWScript.GetBaseItemType(item)))
            {
                result.Add(def);
            }
        }

        return result;
    }
}