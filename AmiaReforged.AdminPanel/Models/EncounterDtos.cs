using System.Text.Json.Serialization;

namespace AmiaReforged.AdminPanel.Models;

// ===================== Enums =====================

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SpawnConditionType
{
    TimeOfDay = 0,
    ChaosThreshold = 1,
    MinPlayerCount = 2,
    MaxPlayerCount = 3,
    RegionTag = 4,
    Custom = 99
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SpawnBonusType
{
    TempHP = 0,
    AC = 1,
    DamageShield = 2,
    Concealment = 3,
    AttackBonus = 4,
    DamageBonus = 5,
    SpellResistance = 6,
    Custom = 99
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MutationEffectType
{
    AbilityBonus = 0,
    ExtraAttack = 1,
    DamageBonus = 2,
    TempHP = 3,
    AC = 4,
    AttackBonus = 5,
    SpellResistance = 6,
    Concealment = 7,
    DamageShield = 8,
    Custom = 99
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NwnAbilityType
{
    Strength = 0,
    Dexterity = 1,
    Constitution = 2,
    Intelligence = 3,
    Wisdom = 4,
    Charisma = 5
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NwnDamageType
{
    Bludgeoning = 1,
    Piercing = 2,
    Slashing = 4,
    Magical = 8,
    Acid = 16,
    Cold = 32,
    Divine = 64,
    Electrical = 128,
    Fire = 256,
    Negative = 512,
    Positive = 1024,
    Sonic = 2048
}

// ===================== Response DTOs =====================

public record SpawnProfileDto(
    Guid Id,
    string AreaResRef,
    string Name,
    bool IsActive,
    int CooldownSeconds,
    int DespawnSeconds,
    int? MaxTotalSpawns,
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

// ===================== Request DTOs =====================

public record CreateProfileRequest(
    string AreaResRef,
    string Name,
    bool IsActive = false,
    int CooldownSeconds = 900,
    int DespawnSeconds = 600,
    int? MaxTotalSpawns = null);

public record UpdateProfileRequest(
    string? Name = null,
    bool? IsActive = null,
    int? CooldownSeconds = null,
    int? DespawnSeconds = null,
    int? MaxTotalSpawns = null);

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

// ===================== Error DTO =====================

public record ApiErrorResponse(string Error, string Detail);

// ===================== Bulk Operations =====================

public record BulkSetActiveRequest(
    List<Guid> Ids,
    bool IsActive);
