using System.Text.Json;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

[ServiceBinding(typeof(IndustryDefinitionLoadingService))]
public class IndustryDefinitionLoadingService(IIndustryRepository repository, IEventBus eventBus) : IDefinitionLoader
{
    private readonly List<FileLoadResult> _failures = [];

    public void Load()
    {
        // Ensure failures are per-load, not cumulative across multiple Load() calls
        _failures.Clear();

        string? resourcePath = Environment.GetEnvironmentVariable("RESOURCE_PATH");
        if (string.IsNullOrEmpty(resourcePath))
        {
            _failures.Add(new FileLoadResult(ResultType.Fail, "RESOURCE_PATH environment variable not set"));
            return;
        }

        string industryDirectory = Path.Combine(resourcePath, "Industries");
        if (!Directory.Exists(industryDirectory))
        {
            _failures.Add(new FileLoadResult(ResultType.Fail, $"Directory does not exist: {industryDirectory}"));
            return;
        }

        // Be robust to extension casing on case-sensitive filesystems
        string[] jsonFiles = Directory
            .EnumerateFiles(industryDirectory, "*", SearchOption.TopDirectoryOnly)
            .Where(f => string.Equals(Path.GetExtension(f), ".json", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        JsonSerializerOptions jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        foreach (string file in jsonFiles)
        {
            string fileName = Path.GetFileName(file);

            try
            {
                string json = File.ReadAllText(file);
                Industry? definition = System.Text.Json.JsonSerializer.Deserialize<Industry>(json, jsonOptions);

                if (definition == null)
                {
                    _failures.Add(new FileLoadResult(ResultType.Fail, "Failed to deserialize definition", fileName));
                    continue;
                }

                if (!TryValidate(definition, out string? error))
                {
                    _failures.Add(new FileLoadResult(ResultType.Fail, error ?? "Validation failed.", fileName));
                    continue;
                }

                // Ensure this matches the repository contract used in tests (method name/signature)
                repository.Add(definition);

                // Publish event
                IndustryRegisteredEvent evt = new(
                    (IndustryTag)definition.Tag,
                    definition.Name,
                    DateTime.UtcNow);
                eventBus.PublishAsync(evt).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _failures.Add(new FileLoadResult(ResultType.Fail, ex.Message, fileName, ex));
            }
        }
    }

    private static bool TryValidate(Industry definition, out string? error)
    {
        if (string.IsNullOrWhiteSpace(definition.Tag))
        {
            error = "Tag must not be empty.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(definition.Name))
        {
            error = "Name must not be empty.";
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
