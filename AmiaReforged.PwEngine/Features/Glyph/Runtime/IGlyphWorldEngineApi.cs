using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Glyph;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem.Glyph;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.Glyph;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime;

/// <summary>Legacy composite contract. New integrations inject a narrow subsystem API;
/// legacy initializers forward to those same capabilities. Do not add domain operations here.</summary>
public interface IGlyphWorldEngineApi : IGlyphIndustryApi, IGlyphKnowledgeApi, IGlyphResourceNodeApi;

/// <summary>
/// Lightweight DTO describing a character's membership in a single industry.
/// </summary>
public record IndustryMembershipInfo(string Tag, string Name, ProficiencyLevel Level);

/// <summary>
/// Lightweight DTO wrapping the character's knowledge point progression state.
/// </summary>
public record KnowledgeProgressionInfo(int TotalKp, int EconomyKp, int LevelUpKp, int AccumulatedProgressionPoints);

/// <summary>
/// Outcome envelope returned by <see cref="IGlyphWorldEngineApi.SpawnResourceNode"/>.
/// <see cref="Success"/> is <c>true</c> when a node was created; otherwise <see cref="FailureReason"/>
/// describes why the spawn was rejected.
/// </summary>
public record SpawnResourceNodeOutcome(bool Success, string? FailureReason, SpawnResourceNodeResult? Result);

/// <summary>
/// Result DTO returned by <see cref="IGlyphWorldEngineApi.SpawnResourceNode"/> on success.
/// Contains everything a downstream Glyph node might need to reference the spawned node.
/// </summary>
public record SpawnResourceNodeResult(
    Guid NodeId,
    string Name,
    string DefinitionTag,
    string QualityLabel,
    int Uses,
    float X,
    float Y,
    float Z);
