using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Effects;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits;

/// <summary>
/// Bidirectional mapper between the <see cref="Trait"/> domain model and
/// the <see cref="PersistedTraitDefinition"/> EF persistence entity.
/// </summary>
[ServiceBinding(typeof(TraitDefinitionMapper))]
public class TraitDefinitionMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Converts a domain <see cref="Trait"/> to a persistence entity.
    /// </summary>
    public PersistedTraitDefinition ToPersistent(Trait trait)
    {
        return new PersistedTraitDefinition
        {
            Tag = trait.Tag,
            Name = trait.Name,
            Description = trait.Description,
            PointCost = trait.PointCost,
            Category = trait.Category,
            DeathBehavior = trait.DeathBehavior,
            RequiresUnlock = trait.RequiresUnlock,
            DmOnly = trait.DmOnly,
            EffectsJson = SerializeEffects(trait.Effects),
            AllowedRacesJson = SerializeList(trait.AllowedRaces),
            AllowedClassesJson = SerializeList(trait.AllowedClasses),
            ForbiddenRacesJson = SerializeList(trait.ForbiddenRaces),
            ForbiddenClassesJson = SerializeList(trait.ForbiddenClasses),
            ConflictingTraitsJson = SerializeList(trait.ConflictingTraits),
            PrerequisiteTraitsJson = SerializeList(trait.PrerequisiteTraits)
        };
    }

    /// <summary>
    /// Converts a persistence entity back to the domain <see cref="Trait"/> model.
    /// </summary>
    public Trait ToDomain(PersistedTraitDefinition persisted)
    {
        return new Trait
        {
            Tag = persisted.Tag,
            Name = persisted.Name,
            Description = persisted.Description,
            PointCost = persisted.PointCost,
            Category = persisted.Category,
            DeathBehavior = persisted.DeathBehavior,
            RequiresUnlock = persisted.RequiresUnlock,
            DmOnly = persisted.DmOnly,
            Effects = DeserializeEffects(persisted.EffectsJson),
            AllowedRaces = DeserializeList(persisted.AllowedRacesJson),
            AllowedClasses = DeserializeList(persisted.AllowedClassesJson),
            ForbiddenRaces = DeserializeList(persisted.ForbiddenRacesJson),
            ForbiddenClasses = DeserializeList(persisted.ForbiddenClassesJson),
            ConflictingTraits = DeserializeList(persisted.ConflictingTraitsJson),
            PrerequisiteTraits = DeserializeList(persisted.PrerequisiteTraitsJson)
        };
    }

    // === Serialization Helpers ===

    private static string SerializeList(List<string> items)
    {
        return JsonSerializer.Serialize(items, JsonOptions);
    }

    private static List<string> DeserializeList(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static string SerializeEffects(List<TraitEffect> effects)
    {
        List<TraitEffectDto> dtos = effects.Select(e => new TraitEffectDto
        {
            EffectType = (int)e.EffectType,
            Target = e.Target,
            Magnitude = e.Magnitude,
            Description = e.Description
        }).ToList();

        return JsonSerializer.Serialize(dtos, JsonOptions);
    }

    private static List<TraitEffect> DeserializeEffects(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            List<TraitEffectDto> dtos = JsonSerializer.Deserialize<List<TraitEffectDto>>(json, JsonOptions) ?? [];
            return dtos.Select(d => new TraitEffect
            {
                EffectType = Enum.IsDefined(typeof(TraitEffectType), d.EffectType) 
                    ? (TraitEffectType)d.EffectType 
                    : TraitEffectType.None,
                Target = d.Target,
                Magnitude = d.Magnitude,
                Description = d.Description
            }).ToList();
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Internal DTO for JSON serialization of trait effects.
    /// </summary>
    private class TraitEffectDto
    {
        public int EffectType { get; set; }
        public string? Target { get; set; }
        public int Magnitude { get; set; }
        public string? Description { get; set; }
    }

    public static TraitDefinitionMapper Create() => new();
}
