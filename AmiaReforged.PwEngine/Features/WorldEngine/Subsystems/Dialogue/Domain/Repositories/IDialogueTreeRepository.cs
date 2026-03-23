using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Repositories;

/// <summary>
/// Repository interface for dialogue tree persistence.
/// </summary>
public interface IDialogueTreeRepository
{
    /// <summary>
    /// Gets a dialogue tree by its unique identifier.
    /// </summary>
    Task<DialogueTree?> GetByIdAsync(DialogueTreeId id, CancellationToken ct = default);

    /// <summary>
    /// Gets all dialogue trees.
    /// </summary>
    Task<List<DialogueTree>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets all dialogue trees assigned to a specific NPC speaker tag.
    /// </summary>
    Task<List<DialogueTree>> GetBySpeakerTagAsync(string speakerTag, CancellationToken ct = default);

    /// <summary>
    /// Saves (creates or updates) a dialogue tree.
    /// </summary>
    Task SaveAsync(DialogueTree tree, CancellationToken ct = default);

    /// <summary>
    /// Deletes a dialogue tree by its unique identifier.
    /// </summary>
    Task<bool> DeleteAsync(DialogueTreeId id, CancellationToken ct = default);

    /// <summary>
    /// Searches dialogue trees by title or ID.
    /// </summary>
    Task<List<DialogueTree>> SearchAsync(string searchTerm, int page = 1, int pageSize = 50,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the total count of dialogue trees matching a search term (for pagination).
    /// </summary>
    Task<int> CountAsync(string? searchTerm = null, CancellationToken ct = default);
}
