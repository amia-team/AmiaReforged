using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Persistence;

/// <summary>
/// Maps between <see cref="Workstation"/> domain objects and
/// <see cref="PersistedWorkstationDefinition"/> EF entities.
/// </summary>
public static class WorkstationMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static PersistedWorkstationDefinition ToEntity(Workstation workstation)
    {
        return new PersistedWorkstationDefinition
        {
            Tag = workstation.Tag,
            Name = workstation.Name,
            Description = workstation.Description,
            PlaceableResRef = workstation.PlaceableResRef,
            AppearanceId = workstation.AppearanceId,
            SupportedIndustriesJson = JsonSerializer.Serialize(
                workstation.SupportedIndustries.Select(t => t.Value).ToList(), JsonOptions),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static Workstation ToDomain(PersistedWorkstationDefinition entity)
    {
        List<string> industryTags = JsonSerializer.Deserialize<List<string>>(entity.SupportedIndustriesJson, JsonOptions)
                                    ?? [];

        return new Workstation
        {
            Tag = new WorkstationTag(entity.Tag),
            Name = entity.Name,
            Description = entity.Description,
            PlaceableResRef = entity.PlaceableResRef,
            AppearanceId = entity.AppearanceId,
            SupportedIndustries = industryTags.Select(t => new IndustryTag(t)).ToList()
        };
    }

    public static void UpdateEntity(PersistedWorkstationDefinition entity, Workstation workstation)
    {
        entity.Name = workstation.Name;
        entity.Description = workstation.Description;
        entity.PlaceableResRef = workstation.PlaceableResRef;
        entity.AppearanceId = workstation.AppearanceId;
        entity.SupportedIndustriesJson = JsonSerializer.Serialize(
            workstation.SupportedIndustries.Select(t => t.Value).ToList(), JsonOptions);
        entity.UpdatedAt = DateTime.UtcNow;
    }
}
