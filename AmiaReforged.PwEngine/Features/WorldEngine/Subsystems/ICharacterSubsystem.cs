using AmiaReforged.PwEngine.Features.WorldEngine.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;

/// <summary>
/// Provides access to character-related operations including registration, stats, and reputation.
/// </summary>
public interface ICharacterSubsystem
{
    // === Character Registration ===

    /// <summary>
    /// Registers a new character in the WorldEngine.
    /// </summary>
    Task<CommandResult> RegisterCharacterAsync(CharacterId characterId, CancellationToken ct = default);

    /// <summary>
    /// Gets character information by ID.
    /// </summary>
    Task<ICharacter?> GetCharacterAsync(CharacterId characterId, CancellationToken ct = default);

    // === Character Stats ===

    /// <summary>
    /// Gets character statistics.
    /// </summary>
    Task<CharacterStats?> GetCharacterStatsAsync(CharacterId characterId, CancellationToken ct = default);

    /// <summary>
    /// Updates character statistics.
    /// </summary>
    Task<CommandResult> UpdateCharacterStatsAsync(CharacterId characterId, CharacterStats stats, CancellationToken ct = default);

    // === Reputation Management ===

    /// <summary>
    /// Gets a character's reputation with an organization.
    /// </summary>
    Task<int> GetReputationAsync(CharacterId characterId, OrganizationId organizationId, CancellationToken ct = default);

    /// <summary>
    /// Adjusts a character's reputation with an organization.
    /// </summary>
    Task<CommandResult> AdjustReputationAsync(
        CharacterId characterId,
        OrganizationId organizationId,
        int adjustment,
        string reason,
        CancellationToken ct = default);

    // === Character Knowledge (Industry/Recipe knowledge) ===

    /// <summary>
    /// Gets character's knowledge repository for industry-related queries.
    /// </summary>
    ICharacterKnowledgeContext GetKnowledgeContext(CharacterId characterId);

    /// <summary>
    /// Gets character's industry membership context.
    /// </summary>
    ICharacterIndustryContext GetIndustryContext(CharacterId characterId);
}

/// <summary>
/// Represents character statistics.
/// </summary>
public record CharacterStats(
    int PlayTime,
    int QuestsCompleted,
    int ItemsCrafted,
    DateTime LastSeen);

