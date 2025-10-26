using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Shops;
using AmiaReforged.PwEngine.Features.WorldEngine.Regions;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy;

[ServiceBinding(typeof(CoinhouseLoader))]
public class CoinhouseLoader(ICoinhouseRepository coinhouses) : IDefinitionLoader
{
    private readonly List<FileLoadResult> _failures = new();

    public void Load()
    {
        string? resourcePath = Environment.GetEnvironmentVariable("RESOURCE_PATH");
        if (string.IsNullOrEmpty(resourcePath))
        {
            _failures.Add(new FileLoadResult(ResultType.Fail, "RESOURCE_PATH environment variable not set"));
            return;
        }

        string regionDir = Path.Combine(resourcePath, "Economy/Coinhouses");

        if (!Directory.Exists(regionDir))
        {
            _failures.Add(new FileLoadResult(ResultType.Fail, $"Directory does not exist: {regionDir}"));
            return;
        }

        string[] jsonFiles = Directory.GetFiles(regionDir, "*.json", SearchOption.AllDirectories);


        foreach (string file in jsonFiles)
        {
            string fileName = Path.GetFileName(file);

            try
            {
                string json = File.ReadAllText(file);
                CoinhouseDefinition? definition =
                    System.Text.Json.JsonSerializer.Deserialize<CoinhouseDefinition>(json);

                if (definition == null)
                {
                    _failures.Add(new FileLoadResult(ResultType.Fail, "Failed to deserialize definition", fileName));
                    continue;
                }

                if (!TryValidate(definition, out string? error))
                {
                    _failures.Add(new FileLoadResult(ResultType.Fail, error, fileName));
                    continue;
                }


                coinhouses.AddNewCoinhouse(new CoinHouse
                {
                    Tag = definition.Tag,
                    Settlement = definition.Settlement,
                    EngineId = Guid.NewGuid()
                });
            }
            catch (Exception ex)
            {
                _failures.Add(new FileLoadResult(ResultType.Fail, ex.Message, fileName));
            }
        }
    }

    private bool TryValidate(CoinhouseDefinition definition, out string? error)
    {
        if (coinhouses.SettlementHasCoinhouse(definition.Settlement))
        {
            error = "Settlement already has a coinhouse.";
            return false;
        }

        if (coinhouses.TagExists(definition.Tag))
        {
            error = "Tag already associated with a coinhouse.";
            return false;
        }

        error = "";
        return true;
    }

    public List<FileLoadResult> Failures()
    {
        throw new NotImplementedException();
    }
}

public record CoinhouseDefinition(string Tag, int Settlement);
