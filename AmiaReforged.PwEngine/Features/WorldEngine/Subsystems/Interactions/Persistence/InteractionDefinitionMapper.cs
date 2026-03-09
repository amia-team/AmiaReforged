using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Persistence;

/// <summary>
/// Static mapper for converting between <see cref="InteractionDefinition"/> domain objects
/// and <see cref="PersistedInteractionDefinition"/> EF entities.
/// Follows the same pattern as <c>IndustryMapper</c> and <c>ResourceNodeMapper</c>.
/// </summary>
public static class InteractionDefinitionMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Converts a domain object to a persistence entity.</summary>
    public static PersistedInteractionDefinition ToEntity(InteractionDefinition definition)
    {
        List<ResponseJsonDto> responseDtos = definition.Responses.Select(r => new ResponseJsonDto
        {
            ResponseTag = r.ResponseTag,
            Weight = r.Weight,
            MinProficiency = r.MinProficiency?.ToString(),
            Message = r.Message,
            Effects = r.Effects.Select(e => new ResponseEffectJsonDto
            {
                EffectType = e.EffectType.ToString(),
                Value = e.Value,
                Metadata = e.Metadata
            }).ToList()
        }).ToList();

        return new PersistedInteractionDefinition
        {
            Tag = definition.Tag,
            Name = definition.Name,
            Description = definition.Description,
            TargetMode = definition.TargetMode.ToString(),
            BaseRounds = definition.BaseRounds,
            MinRounds = definition.MinRounds,
            ProficiencyReducesRounds = definition.ProficiencyReducesRounds,
            RequiresIndustryMembership = definition.RequiresIndustryMembership,
            ResponsesJson = JsonSerializer.Serialize(responseDtos, JsonOptions),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Converts a persistence entity to a domain object.</summary>
    public static InteractionDefinition ToDomain(PersistedInteractionDefinition entity)
    {
        Enum.TryParse<InteractionTargetMode>(entity.TargetMode, true, out var targetMode);

        List<ResponseJsonDto> responseDtos =
            JsonSerializer.Deserialize<List<ResponseJsonDto>>(entity.ResponsesJson, JsonOptions) ?? [];

        List<InteractionResponse> responses = responseDtos.Select(r =>
        {
            ProficiencyLevel? minProf = null;
            if (!string.IsNullOrEmpty(r.MinProficiency))
            {
                Enum.TryParse<ProficiencyLevel>(r.MinProficiency, true, out var parsed);
                minProf = parsed;
            }

            return new InteractionResponse
            {
                ResponseTag = r.ResponseTag ?? string.Empty,
                Weight = r.Weight,
                MinProficiency = minProf,
                Message = r.Message,
                Effects = r.Effects?.Select(e =>
                {
                    Enum.TryParse<InteractionResponseEffectType>(e.EffectType, true, out var effectType);
                    return new InteractionResponseEffect
                    {
                        EffectType = effectType,
                        Value = e.Value ?? string.Empty,
                        Metadata = e.Metadata ?? new Dictionary<string, object>()
                    };
                }).ToList() ?? []
            };
        }).ToList();

        return new InteractionDefinition
        {
            Tag = entity.Tag,
            Name = entity.Name,
            Description = entity.Description,
            TargetMode = targetMode,
            BaseRounds = entity.BaseRounds,
            MinRounds = entity.MinRounds,
            ProficiencyReducesRounds = entity.ProficiencyReducesRounds,
            RequiresIndustryMembership = entity.RequiresIndustryMembership,
            Responses = responses
        };
    }

    /// <summary>Updates an existing entity from a domain object in-place.</summary>
    public static void UpdateEntity(PersistedInteractionDefinition entity, InteractionDefinition definition)
    {
        entity.Name = definition.Name;
        entity.Description = definition.Description;
        entity.TargetMode = definition.TargetMode.ToString();
        entity.BaseRounds = definition.BaseRounds;
        entity.MinRounds = definition.MinRounds;
        entity.ProficiencyReducesRounds = definition.ProficiencyReducesRounds;
        entity.RequiresIndustryMembership = definition.RequiresIndustryMembership;

        List<ResponseJsonDto> responseDtos = definition.Responses.Select(r => new ResponseJsonDto
        {
            ResponseTag = r.ResponseTag,
            Weight = r.Weight,
            MinProficiency = r.MinProficiency?.ToString(),
            Message = r.Message,
            Effects = r.Effects.Select(e => new ResponseEffectJsonDto
            {
                EffectType = e.EffectType.ToString(),
                Value = e.Value,
                Metadata = e.Metadata
            }).ToList()
        }).ToList();

        entity.ResponsesJson = JsonSerializer.Serialize(responseDtos, JsonOptions);
        entity.UpdatedAt = DateTime.UtcNow;
    }

    // === Internal DTO classes for JSON serialization shape ===

    private class ResponseJsonDto
    {
        public string? ResponseTag { get; set; }
        public int Weight { get; set; } = 1;
        public string? MinProficiency { get; set; }
        public string? Message { get; set; }
        public List<ResponseEffectJsonDto>? Effects { get; set; }
    }

    private class ResponseEffectJsonDto
    {
        public string? EffectType { get; set; }
        public string? Value { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }
}
