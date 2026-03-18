using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime;

/// <summary>
/// Read-only facade that exposes World Engine industry and knowledge data to Glyph node executors.
/// Set on <see cref="GlyphExecutionContext.WorldEngine"/> by the hook service that creates the context.
/// Implementations wrap the industry/membership/knowledge repositories and services so that
/// node executors remain stateless POCOs with no direct dependency injection.
/// </summary>
public interface IGlyphWorldEngineApi
{
    /// <summary>
    /// Returns all industry memberships for a character, including tag, display name, and proficiency level.
    /// Returns an empty list if the character is not enrolled in any industry or the character ID is invalid.
    /// </summary>
    List<IndustryMembershipInfo> GetIndustryMemberships(Guid characterId);

    /// <summary>
    /// Returns the character's proficiency level in a specific industry, or <c>null</c> if not enrolled.
    /// </summary>
    ProficiencyLevel? GetIndustryLevel(Guid characterId, string industryTag);

    /// <summary>
    /// Returns <c>true</c> if the character is a member of the specified industry.
    /// </summary>
    bool IsIndustryMember(Guid characterId, string industryTag);

    /// <summary>
    /// Returns the tags of all knowledge the character has learned across all industries.
    /// Returns an empty list if the character has no learned knowledge.
    /// </summary>
    List<string> GetLearnedKnowledgeTags(Guid characterId);

    /// <summary>
    /// Returns <c>true</c> if the character has learned the specified knowledge article.
    /// </summary>
    bool HasKnowledge(Guid characterId, string knowledgeTag);

    /// <summary>
    /// Returns <c>true</c> if the character has learned knowledge whose effects include
    /// <c>UnlockInteraction</c> targeting the specified interaction tag.
    /// </summary>
    bool HasUnlockedInteraction(Guid characterId, string interactionTag);

    /// <summary>
    /// Returns the character's knowledge point progression snapshot.
    /// Returns a zeroed-out record if the character has no progression data.
    /// </summary>
    KnowledgeProgressionInfo GetKnowledgeProgression(Guid characterId);
}

/// <summary>
/// Lightweight DTO describing a character's membership in a single industry.
/// </summary>
public record IndustryMembershipInfo(string Tag, string Name, ProficiencyLevel Level);

/// <summary>
/// Lightweight DTO wrapping the character's knowledge point progression state.
/// </summary>
public record KnowledgeProgressionInfo(int TotalKp, int EconomyKp, int LevelUpKp, int AccumulatedProgressionPoints);
