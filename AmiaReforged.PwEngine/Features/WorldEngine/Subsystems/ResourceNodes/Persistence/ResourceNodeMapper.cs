using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.Persistence;

/// <summary>
/// Maps between <see cref="ResourceNodeDefinition"/> domain records and
/// <see cref="PersistedResourceNodeDefinition"/> EF entities.
/// </summary>
public static class ResourceNodeMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static PersistedResourceNodeDefinition ToEntity(ResourceNodeDefinition definition)
    {
        return new PersistedResourceNodeDefinition
        {
            Tag = definition.Tag,
            Name = definition.Name,
            Description = definition.Description,
            PlcAppearance = definition.PlcAppearance,
            Type = definition.Type.ToString(),
            Uses = definition.Uses,
            BaseHarvestRounds = definition.BaseHarvestRounds,
            RequirementJson = JsonSerializer.Serialize(definition.Requirement, JsonOptions),
            OutputsJson = JsonSerializer.Serialize(definition.Outputs, JsonOptions),
            FloraPropertiesJson = definition.FloraProperties != null
                ? JsonSerializer.Serialize(definition.FloraProperties, JsonOptions)
                : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static ResourceNodeDefinition ToDomain(PersistedResourceNodeDefinition entity)
    {
        Enum.TryParse<ResourceType>(entity.Type, true, out var resourceType);

        HarvestContext requirement = JsonSerializer.Deserialize<HarvestContext>(entity.RequirementJson, JsonOptions)
                                     ?? new HarvestContext(JobSystemItemType.None);

        HarvestOutput[] outputs = JsonSerializer.Deserialize<HarvestOutput[]>(entity.OutputsJson, JsonOptions)
                                  ?? Array.Empty<HarvestOutput>();

        FloraProperties? floraProps = null;
        if (!string.IsNullOrEmpty(entity.FloraPropertiesJson))
        {
            floraProps = JsonSerializer.Deserialize<FloraProperties>(entity.FloraPropertiesJson, JsonOptions);
        }

        return new ResourceNodeDefinition(
            PlcAppearance: entity.PlcAppearance,
            Type: resourceType,
            Tag: entity.Tag,
            Requirement: requirement,
            Outputs: outputs,
            Uses: entity.Uses,
            BaseHarvestRounds: entity.BaseHarvestRounds,
            Name: entity.Name,
            Description: entity.Description,
            FloraProperties: floraProps);
    }

    /// <summary>
    /// Updates an existing entity with values from a domain record (preserves CreatedAt).
    /// </summary>
    public static void UpdateEntity(PersistedResourceNodeDefinition entity, ResourceNodeDefinition definition)
    {
        entity.Name = definition.Name;
        entity.Description = definition.Description;
        entity.PlcAppearance = definition.PlcAppearance;
        entity.Type = definition.Type.ToString();
        entity.Uses = definition.Uses;
        entity.BaseHarvestRounds = definition.BaseHarvestRounds;
        entity.RequirementJson = JsonSerializer.Serialize(definition.Requirement, JsonOptions);
        entity.OutputsJson = JsonSerializer.Serialize(definition.Outputs, JsonOptions);
        entity.FloraPropertiesJson = definition.FloraProperties != null
            ? JsonSerializer.Serialize(definition.FloraProperties, JsonOptions)
            : null;
        entity.UpdatedAt = DateTime.UtcNow;
    }
}
