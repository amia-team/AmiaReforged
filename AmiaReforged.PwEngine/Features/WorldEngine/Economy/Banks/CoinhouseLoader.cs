using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Banks;

[ServiceBinding(typeof(CoinhouseLoader))]
public class CoinhouseLoader(ICoinhouseRepository coinhouses, IRegionRepository regions) : IDefinitionLoader
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

                // Strong types are created from JSON primitives and validated
                var coinhouseTag = new CoinhouseTag(definition.Tag);
                var settlementId = SettlementId.Parse(definition.Settlement);

                coinhouses.AddNewCoinhouse(new CoinHouse
                {
                    Tag = coinhouseTag,  // Implicit conversion to string
                    Settlement = settlementId,  // Implicit conversion to int
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
        // Validate and create strong types
        SettlementId settlementId;
        CoinhouseTag coinhouseTag;

        try
        {
            settlementId = SettlementId.Parse(definition.Settlement);
            coinhouseTag = new CoinhouseTag(definition.Tag);
        }
        catch (ArgumentException ex)
        {
            error = ex.Message;
            return false;
        }

        if (coinhouses.SettlementHasCoinhouse(settlementId))
        {
            error = "Settlement already has a coinhouse.";
            return false;
        }

        if (coinhouses.TagExists(coinhouseTag))
        {
            error = "Tag already associated with a coinhouse.";
            return false;
        }

        if (!regions.TryGetRegionBySettlement(settlementId, out _))
        {
            error = "Settlement is not defined in any region.";
            return false;
        }

        error = "";
        return true;
    }

    public List<FileLoadResult> Failures()
    {
        return _failures;
    }
}

public record CoinhouseDefinition(string Tag, int Settlement);
