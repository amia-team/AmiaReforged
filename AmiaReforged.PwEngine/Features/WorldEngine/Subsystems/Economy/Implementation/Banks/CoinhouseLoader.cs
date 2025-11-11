using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks;

[ServiceBinding(typeof(CoinhouseLoader))]
public class CoinhouseLoader(ICoinhouseRepository coinhouses, IRegionRepository regions) : IDefinitionLoader
{
    private readonly List<FileLoadResult> _failures = new();
    private readonly HashSet<int> _seenSettlements = new();
    private readonly HashSet<string> _seenTags = new(StringComparer.OrdinalIgnoreCase);

    public void Load()
    {
        _failures.Clear();
        _seenSettlements.Clear();
        _seenTags.Clear();

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
        Array.Sort(jsonFiles, StringComparer.OrdinalIgnoreCase);


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

                if (!TryProcessDefinition(definition, fileName))
                {
                    continue;
                }
            }
            catch (Exception ex)
            {
                _failures.Add(new FileLoadResult(ResultType.Fail, ex.Message, fileName));
            }
        }
    }

    private bool TryProcessDefinition(CoinhouseDefinition definition, string fileName)
    {
        SettlementId settlementId;
        CoinhouseTag coinhouseTag;

        try
        {
            settlementId = SettlementId.Parse(definition.Settlement);
            coinhouseTag = new CoinhouseTag(definition.Tag);
        }
        catch (ArgumentException ex)
        {
            _failures.Add(new FileLoadResult(ResultType.Fail, ex.Message, fileName));
            return false;
        }

        if (!_seenSettlements.Add(settlementId.Value))
        {
            _failures.Add(new FileLoadResult(ResultType.Fail,
                $"Duplicate coinhouse settlement '{settlementId.Value}' detected in definitions.", fileName));
            return false;
        }

        if (!_seenTags.Add(coinhouseTag.Value))
        {
            _failures.Add(new FileLoadResult(ResultType.Fail,
                $"Duplicate coinhouse tag '{coinhouseTag.Value}' detected in definitions.", fileName));
            return false;
        }

        if (!regions.TryGetRegionBySettlement(settlementId, out _))
        {
            _failures.Add(new FileLoadResult(ResultType.Fail,
                $"Settlement {settlementId.Value} is not defined in any region.", fileName));
            return false;
        }

        CoinHouse? existingForSettlement = coinhouses.GetSettlementCoinhouse(settlementId);
        if (existingForSettlement is not null)
        {
            if (!existingForSettlement.Tag.Equals(coinhouseTag.Value, StringComparison.OrdinalIgnoreCase))
            {
                _failures.Add(new FileLoadResult(ResultType.Fail,
                    $"Settlement {settlementId.Value} already mapped to coinhouse '{existingForSettlement.Tag}'.",
                    fileName));
                return false;
            }

            // Already registered with matching mapping; nothing to add.
            return true;
        }

        CoinHouse? existingForTag = coinhouses.GetCoinhouseByTag(coinhouseTag);
        if (existingForTag is not null)
        {
            if (existingForTag.Settlement != settlementId.Value)
            {
                _failures.Add(new FileLoadResult(ResultType.Fail,
                    $"Coinhouse tag '{coinhouseTag.Value}' already assigned to settlement {existingForTag.Settlement}.",
                    fileName));
                return false;
            }

            return true;
        }

        CoinHouse newCoinhouse = new()
        {
            Tag = coinhouseTag,
            Settlement = settlementId,
            EngineId = Guid.NewGuid(),
            PersonaIdString = PersonaId.FromCoinhouse(coinhouseTag).ToString()
        };

        coinhouses.AddNewCoinhouse(newCoinhouse);
        return true;
    }

    public List<FileLoadResult> Failures()
    {
        return _failures;
    }
}

public record CoinhouseDefinition(string Tag, int Settlement);
