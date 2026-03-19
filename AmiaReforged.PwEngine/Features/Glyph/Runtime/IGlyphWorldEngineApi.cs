using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime;

/// <summary>
/// Facade that exposes World Engine data and operations to Glyph node executors.
/// Set on <see cref="GlyphExecutionContext.WorldEngine"/> by the hook service that creates the context.
/// Implementations wrap the industry/membership/knowledge repositories and resource node services
/// so that node executors remain stateless POCOs with no direct dependency injection.
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

    // ── Resource Node Operations ──────────────────────────────────────────

    /// <summary>
    /// Spawns a single resource node inside a trigger zone, using the area's resource definitions
    /// filtered by the trigger's <c>node_tags</c> local variable (comma-separated type names).
    /// <para>
    /// The method resolves the trigger from its NWN object handle, validates it is tagged
    /// <c>worldengine_node_region</c>, reads the type filter from the trigger's local variables,
    /// derives the area from the trigger's location, looks up the matching
    /// <see cref="AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions.AreaDefinition"/>,
    /// filters its definition tags by matching resource type, randomly selects one definition,
    /// generates a walkable position inside the trigger, and creates + spawns the node.
    /// </para>
    /// </summary>
    /// <param name="triggerHandle">The raw NWN object handle of the trigger to spawn within.</param>
    /// <returns>A <see cref="SpawnResourceNodeResult"/> on success, or <c>null</c> if spawning failed
    /// (wrong tag, missing definitions, invalid trigger, etc. — reason logged server-side).</returns>
    SpawnResourceNodeResult? SpawnResourceNode(uint triggerHandle);
}

/// <summary>
/// Lightweight DTO describing a character's membership in a single industry.
/// </summary>
public record IndustryMembershipInfo(string Tag, string Name, ProficiencyLevel Level);

/// <summary>
/// Lightweight DTO wrapping the character's knowledge point progression state.
/// </summary>
public record KnowledgeProgressionInfo(int TotalKp, int EconomyKp, int LevelUpKp, int AccumulatedProgressionPoints);

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
