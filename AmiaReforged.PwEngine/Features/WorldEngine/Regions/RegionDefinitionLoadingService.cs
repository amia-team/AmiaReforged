using Anvil.Services;
using Microsoft.IdentityModel.Tokens;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Regions;

[ServiceBinding(typeof(RegionDefinitionLoadingService))]
public class RegionDefinitionLoadingService(IRegionRepository repository) : IDefinitionLoader
{
    private readonly List<FileLoadResult> _failures = new();
    private readonly HashSet<int> _seenSettlements = new();

    public void Load()
    {
        _seenSettlements.Clear();

        string? resourcePath = Environment.GetEnvironmentVariable("RESOURCE_PATH");
        if (string.IsNullOrEmpty(resourcePath))
        {
            _failures.Add(new FileLoadResult(ResultType.Fail, "RESOURCE_PATH environment variable not set"));
            return;
        }

        string regionDir = Path.Combine(resourcePath, "Regions");

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
                RegionDefinition? definition =
                    System.Text.Json.JsonSerializer.Deserialize<RegionDefinition>(json);

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

                repository.Add(definition);

                // Index settlements for cross-file duplicate detection
                foreach (int sid in definition.Settlements)
                {
                    _seenSettlements.Add(sid);
                }
            }
            catch (Exception ex)
            {
                _failures.Add(new FileLoadResult(ResultType.Fail, ex.Message, fileName));
            }
        }
    }

    private bool TryValidate(RegionDefinition definition, out string? error)
    {
        if (definition.Tag.IsNullOrEmpty())
        {
            error = "Tag must not be empty.";
            return false;
        }

        if (definition.Areas.IsNullOrEmpty())
        {
            error = "Areas must not be empty.";
            return false;
        }

        if (definition.Areas.Any(a => a.ResRef.IsNullOrEmpty()))
        {
            error = "Area ResRef must not be empty.";
            return false;
        }


        if (definition.Areas.Any(a => a.ResRef.Length > 16))
        {
            error = "Area ResRefs must not exceed 16 characters.";
            return false;
        }

        if (definition.Name.IsNullOrEmpty())
        {
            error = "Name must not be empty.";
            return false;
        }

        if (definition.Settlements is { Count: > 0 } && definition.Settlements.Any(s => s < 0))
        {
            error = "Settlement IDs must be non-negative integers.";
            return false;
        }

        // Reject intra-file duplicates
        if (definition.Settlements.Count != definition.Settlements.Distinct().Count())
        {
            error = "Duplicate settlement IDs within the same region definition are not allowed.";
            return false;
        }

        // Reject cross-file duplicates
        List<int> duplicates = definition.Settlements.Where(s => _seenSettlements.Contains(s)).Distinct().ToList();
        if (duplicates.Count > 0)
        {
            string dupList = string.Join(", ", duplicates);
            string details = string.Empty;
            if (repository.TryGetRegionBySettlement(duplicates[0], out RegionDefinition? existing) && existing is not null)
            {
                details = $" (already assigned to region '{existing.Tag}')";
            }
            error = $"Settlement ID(s) [{dupList}] already assigned to another region{details}.";
            return false;
        }

        error = null;
        return true;
    }

    public List<FileLoadResult> Failures()
    {
        return _failures;
    }
}
