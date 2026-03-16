using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;

/// <summary>
/// Repository for persisting and retrieving <see cref="KnowledgeProgression"/> entities.
/// One KnowledgeProgression exists per character (keyed by CharacterId).
/// </summary>
public interface IKnowledgeProgressionRepository
{
    /// <summary>
    /// Gets the progression record for a character, or null if none exists.
    /// </summary>
    KnowledgeProgression? GetByCharacterId(Guid characterId);

    /// <summary>
    /// Gets the progression record for a character, creating a default one if none exists.
    /// </summary>
    KnowledgeProgression GetOrCreate(Guid characterId);

    /// <summary>
    /// Persists changes to a progression record.
    /// </summary>
    void Update(KnowledgeProgression progression);

    /// <summary>
    /// Adds a new progression record.
    /// </summary>
    void Add(KnowledgeProgression progression);
}
