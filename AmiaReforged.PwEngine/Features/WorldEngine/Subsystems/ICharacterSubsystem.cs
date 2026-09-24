using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;

/// <summary>
/// Provides access to character-related operations including stats, knowledge, and reputation.
/// </summary>
public interface ICharacterSubsystem
{
    /// <summary>
    /// Gets character information by ID.
    /// </summary>
    Task<ICharacter?> GetCharacterAsync(CharacterId characterId, CancellationToken ct = default);

    // === Character Stats ===

    /// <summary>
    /// Gets character statistics.
    /// </summary>
    /// <remarks>
    /// Returns the truthful statistics projection. Only total <see
    /// cref="CharacterStats.PlayTime">play time</see> has a backing data source.
    /// The engine once reported rank-ups as quests, industries joined as crafted
    /// items, and the current time as last seen; none of those are real, so the
    /// projection exposes just the one supported field and returns <c>null</c>
    /// when the character has no statistics record.
    /// </remarks>
    Task<CharacterStats?> GetCharacterStatsAsync(CharacterId characterId, CancellationToken ct = default);

    /// <summary>
    /// Updates character statistics.
    /// </summary>
    /// <remarks>
    /// Only <see cref="CharacterStats.PlayTime">play time</see> is a supported,
    /// writable field. The update is dispatched through the command system and
    /// fails when the character has no statistics record.
    /// </remarks>
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
/// The truthful character statistics projection.
///
/// Only <see cref="PlayTime"/> is backed by real data. Every other statistic the
/// engine historically reported was a mislabeled value (rank-ups labelled as
/// quests, industries joined labelled as crafted items, "now" labelled as last
/// seen). Rather than continue to surface those approximations, this record
/// exposes just the single supported, writable field. See task 022 for the
/// contract decision.
/// </summary>
public record CharacterStats(int PlayTime);

