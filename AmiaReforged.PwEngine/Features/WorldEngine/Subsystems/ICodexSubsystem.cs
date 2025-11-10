using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;

/// <summary>
/// Provides access to codex-related operations including knowledge management and lore.
/// </summary>
public interface ICodexSubsystem
{
    // === Knowledge Entries ===

    /// <summary>
    /// Gets a knowledge entry by ID.
    /// </summary>
    Task<KnowledgeEntry?> GetKnowledgeEntryAsync(string entryId, CancellationToken ct = default);

    /// <summary>
    /// Searches knowledge entries by text or tags.
    /// </summary>
    Task<List<KnowledgeEntry>> SearchKnowledgeAsync(
        string searchTerm,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all knowledge entries in a category.
    /// </summary>
    Task<List<KnowledgeEntry>> GetKnowledgeByCategoryAsync(
        KnowledgeCategory category,
        CancellationToken ct = default);

    // === Character Knowledge ===

    /// <summary>
    /// Grants knowledge of an entry to a character.
    /// </summary>
    Task<CommandResult> GrantKnowledgeAsync(
        CharacterId characterId,
        string entryId,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if a character has knowledge of an entry.
    /// </summary>
    Task<bool> HasKnowledgeAsync(
        CharacterId characterId,
        string entryId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all knowledge entries known by a character.
    /// </summary>
    Task<List<KnowledgeEntry>> GetCharacterKnowledgeAsync(
        CharacterId characterId,
        CancellationToken ct = default);

    // === Lore Management ===

    /// <summary>
    /// Creates a new knowledge entry.
    /// </summary>
    Task<CommandResult> CreateKnowledgeEntryAsync(
        CreateKnowledgeEntryCommand command,
        CancellationToken ct = default);

    /// <summary>
    /// Updates an existing knowledge entry.
    /// </summary>
    Task<CommandResult> UpdateKnowledgeEntryAsync(
        UpdateKnowledgeEntryCommand command,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a knowledge entry.
    /// </summary>
    Task<CommandResult> DeleteKnowledgeEntryAsync(
        string entryId,
        CancellationToken ct = default);
}

/// <summary>
/// Represents a knowledge entry in the codex.
/// </summary>
public record KnowledgeEntry(
    string EntryId,
    string Title,
    string Content,
    KnowledgeCategory Category,
    List<string> Tags,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

/// <summary>
/// Categories of knowledge in the codex.
/// </summary>
public enum KnowledgeCategory
{
    History,
    Geography,
    Magic,
    Religion,
    Nature,
    Culture,
    Organizations,
    Creatures,
    Items,
    Persons,
    Events,
    Legends,
    Secrets
}

/// <summary>
/// Command to create a new knowledge entry.
/// </summary>
public record CreateKnowledgeEntryCommand(
    string EntryId,
    string Title,
    string Content,
    KnowledgeCategory Category,
    List<string> Tags);

/// <summary>
/// Command to update a knowledge entry.
/// </summary>
public record UpdateKnowledgeEntryCommand(
    string EntryId,
    string? Title = null,
    string? Content = null,
    KnowledgeCategory? Category = null,
    List<string>? Tags = null);

