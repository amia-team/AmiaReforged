using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.Persistence;

/// <summary>
/// Maps between <see cref="ItemBlueprint"/> domain records and <see cref="PersistedItemBlueprint"/> EF entities.
/// </summary>
public static class ItemBlueprintMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static PersistedItemBlueprint ToEntity(ItemBlueprint blueprint)
    {
        return new PersistedItemBlueprint
        {
            ResRef = blueprint.ResRef,
            ItemTag = blueprint.ItemTag,
            Name = blueprint.Name,
            Description = blueprint.Description,
            BaseItemType = blueprint.BaseItemType,
            BaseValue = blueprint.BaseValue,
            WeightIncreaseConstant = blueprint.WeightIncreaseConstant,
            JobSystemType = blueprint.JobSystemType.ToString(),
            MaterialsJson = JsonSerializer.Serialize(
                blueprint.Materials.Select(m => m.ToString()).ToArray(), JsonOptions),
            AppearanceJson = JsonSerializer.Serialize(blueprint.Appearance, JsonOptions),
            LocalVariablesJson = blueprint.LocalVariables != null
                ? JsonSerializer.Serialize(blueprint.LocalVariables, JsonOptions)
                : null,
            SourceFile = blueprint.SourceFile,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static ItemBlueprint ToDomain(PersistedItemBlueprint entity)
    {
        MaterialEnum[] materials = Array.Empty<MaterialEnum>();
        if (!string.IsNullOrEmpty(entity.MaterialsJson))
        {
            string[]? materialStrings = JsonSerializer.Deserialize<string[]>(entity.MaterialsJson, JsonOptions);
            if (materialStrings != null)
            {
                materials = materialStrings
                    .Select(s => Enum.TryParse<MaterialEnum>(s, true, out var m) ? m : MaterialEnum.None)
                    .ToArray();
            }
        }

        AppearanceData appearance = JsonSerializer.Deserialize<AppearanceData>(entity.AppearanceJson, JsonOptions)
                                    ?? new AppearanceData(0, null, null);

        IReadOnlyList<JsonLocalVariableDefinition>? localVars = null;
        if (!string.IsNullOrEmpty(entity.LocalVariablesJson))
        {
            localVars = JsonSerializer.Deserialize<List<JsonLocalVariableDefinition>>(
                entity.LocalVariablesJson, JsonOptions);
        }

        Enum.TryParse<JobSystemItemType>(entity.JobSystemType, true, out var jobType);

        return new ItemBlueprint(
            ResRef: entity.ResRef,
            ItemTag: entity.ItemTag,
            Name: entity.Name,
            Description: entity.Description,
            Materials: materials,
            JobSystemType: jobType,
            BaseItemType: entity.BaseItemType,
            Appearance: appearance,
            LocalVariables: localVars,
            BaseValue: entity.BaseValue,
            WeightIncreaseConstant: entity.WeightIncreaseConstant)
        {
            SourceFile = entity.SourceFile
        };
    }

    /// <summary>
    /// Updates an existing entity with values from a domain record (preserves CreatedAt).
    /// </summary>
    public static void UpdateEntity(PersistedItemBlueprint entity, ItemBlueprint blueprint)
    {
        entity.ResRef = blueprint.ResRef;
        entity.Name = blueprint.Name;
        entity.Description = blueprint.Description;
        entity.BaseItemType = blueprint.BaseItemType;
        entity.BaseValue = blueprint.BaseValue;
        entity.WeightIncreaseConstant = blueprint.WeightIncreaseConstant;
        entity.JobSystemType = blueprint.JobSystemType.ToString();
        entity.MaterialsJson = JsonSerializer.Serialize(
            blueprint.Materials.Select(m => m.ToString()).ToArray(), JsonOptions);
        entity.AppearanceJson = JsonSerializer.Serialize(blueprint.Appearance, JsonOptions);
        entity.LocalVariablesJson = blueprint.LocalVariables != null
            ? JsonSerializer.Serialize(blueprint.LocalVariables, JsonOptions)
            : null;
        entity.SourceFile = blueprint.SourceFile;
        entity.UpdatedAt = DateTime.UtcNow;
    }
}
