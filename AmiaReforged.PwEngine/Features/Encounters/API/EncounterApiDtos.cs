using System.Text.Json;
using AmiaReforged.PwEngine.Features.Encounters.Models;
using AmiaReforged.PwEngine.Features.Encounters.Services;
using AmiaReforged.PwEngine.Features.WorldEngine.API;

namespace AmiaReforged.PwEngine.Features.Encounters.API;

/// <summary>
/// DTO models used for JSON serialization in the encounter API.
/// </summary>
public static class EncounterApiDtos
{
    public record SpawnProfileDto(
        Guid Id,
        string AreaResRef,
        string Name,
        bool IsActive,
        int CooldownSeconds,
        int DespawnSeconds,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        List<SpawnGroupDto> SpawnGroups,
        List<SpawnBonusDto> Bonuses,
        MiniBossConfigDto? MiniBoss);

    public record SpawnGroupDto(
        Guid Id,
        string Name,
        int Weight,
        List<SpawnConditionDto> Conditions,
        List<SpawnEntryDto> Entries);

    public record SpawnConditionDto(
        Guid Id,
        SpawnConditionType Type,
        string Operator,
        string Value);

    public record SpawnEntryDto(
        Guid Id,
        string CreatureResRef,
        int RelativeWeight,
        int MinCount,
        int MaxCount);

    public record SpawnBonusDto(
        Guid Id,
        string Name,
        SpawnBonusType Type,
        int Value,
        int DurationSeconds,
        bool IsActive);

    public record MiniBossConfigDto(
        Guid Id,
        string CreatureResRef,
        int SpawnChancePercent,
        List<SpawnBonusDto> Bonuses);

    // --- Request DTOs ---

    public record CreateProfileRequest(
        string AreaResRef,
        string Name,
        bool IsActive = false,
        int CooldownSeconds = 900,
        int DespawnSeconds = 600);

    public record UpdateProfileRequest(
        string? Name = null,
        bool? IsActive = null,
        int? CooldownSeconds = null,
        int? DespawnSeconds = null);

    public record CreateGroupRequest(
        string Name,
        int Weight = 1,
        List<CreateConditionRequest>? Conditions = null,
        List<CreateEntryRequest>? Entries = null);

    public record CreateConditionRequest(
        SpawnConditionType Type,
        string Operator,
        string Value);

    public record CreateEntryRequest(
        string CreatureResRef,
        int RelativeWeight = 1,
        int MinCount = 1,
        int MaxCount = 4);

    public record UpdateGroupRequest(
        string? Name = null,
        int? Weight = null);

    public record UpdateEntryRequest(
        string? CreatureResRef = null,
        int? RelativeWeight = null,
        int? MinCount = null,
        int? MaxCount = null);

    public record UpdateConditionRequest(
        SpawnConditionType? Type = null,
        string? Operator = null,
        string? Value = null);

    public record CreateBonusRequest(
        string Name,
        SpawnBonusType Type,
        int Value,
        int DurationSeconds = 0,
        bool IsActive = true);

    public record UpdateBonusRequest(
        string? Name = null,
        SpawnBonusType? Type = null,
        int? Value = null,
        int? DurationSeconds = null,
        bool? IsActive = null);

    public record CreateMiniBossRequest(
        string CreatureResRef,
        int SpawnChancePercent = 5);

    public record UpdateMiniBossRequest(
        string? CreatureResRef = null,
        int? SpawnChancePercent = null);

    // --- Mutation DTOs ---

    public record MutationTemplateDto(
        Guid Id,
        string Prefix,
        string? Description,
        int SpawnChancePercent,
        bool IsActive,
        List<MutationEffectDto> Effects);

    public record MutationEffectDto(
        Guid Id,
        MutationEffectType Type,
        int Value,
        NwnAbilityType? AbilityType,
        NwnDamageType? DamageType,
        int DurationSeconds,
        bool IsActive);

    public record CreateMutationRequest(
        string Prefix,
        string? Description = null,
        int SpawnChancePercent = 10,
        bool IsActive = true);

    public record UpdateMutationRequest(
        string? Prefix = null,
        string? Description = null,
        int? SpawnChancePercent = null,
        bool? IsActive = null);

    public record CreateMutationEffectRequest(
        MutationEffectType Type,
        int Value,
        NwnAbilityType? AbilityType = null,
        NwnDamageType? DamageType = null,
        int DurationSeconds = 0,
        bool IsActive = true);

    public record UpdateMutationEffectRequest(
        MutationEffectType? Type = null,
        int? Value = null,
        NwnAbilityType? AbilityType = null,
        NwnDamageType? DamageType = null,
        int? DurationSeconds = null,
        bool? IsActive = null);

    // --- Mapping ---

    public static SpawnProfileDto ToDto(SpawnProfile p) => new(
        p.Id, p.AreaResRef, p.Name, p.IsActive,
        p.CooldownSeconds, p.DespawnSeconds,
        p.CreatedAt, p.UpdatedAt,
        p.SpawnGroups.Select(ToDto).ToList(),
        p.Bonuses.Select(ToDto).ToList(),
        p.MiniBoss != null ? ToDto(p.MiniBoss) : null);

    public static SpawnGroupDto ToDto(SpawnGroup g) => new(
        g.Id, g.Name, g.Weight,
        g.Conditions.Select(ToDto).ToList(),
        g.Entries.Select(ToDto).ToList());

    public static SpawnConditionDto ToDto(SpawnCondition c) => new(
        c.Id, c.Type, c.Operator, c.Value);

    public static SpawnEntryDto ToDto(SpawnEntry e) => new(
        e.Id, e.CreatureResRef, e.RelativeWeight, e.MinCount, e.MaxCount);

    public static SpawnBonusDto ToDto(SpawnBonus b) => new(
        b.Id, b.Name, b.Type, b.Value, b.DurationSeconds, b.IsActive);

    public static MiniBossConfigDto ToDto(MiniBossConfig m) => new(
        m.Id, m.CreatureResRef, m.SpawnChancePercent,
        m.Bonuses.Select(ToDto).ToList());

    public static MutationTemplateDto ToMutationDto(MutationTemplate t) => new(
        t.Id, t.Prefix, t.Description, t.SpawnChancePercent, t.IsActive,
        t.Effects.Select(ToMutationEffectDto).ToList());

    public static MutationEffectDto ToMutationEffectDto(MutationEffect e) => new(
        e.Id, e.Type, e.Value, e.AbilityType, e.DamageType, e.DurationSeconds, e.IsActive);
}
