namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;

/// <summary>
/// Repository for persisting and retrieving <see cref="KnowledgeCapProfile"/> entities.
/// Cap profiles define custom soft/hard caps for economy-earned knowledge points.
/// </summary>
public interface IKnowledgeCapProfileRepository
{
    /// <summary>
    /// Gets all defined cap profiles.
    /// </summary>
    List<KnowledgeCapProfile> GetAll();

    /// <summary>
    /// Gets a cap profile by its unique tag, or null if not found.
    /// </summary>
    KnowledgeCapProfile? GetByTag(string tag);

    /// <summary>
    /// Adds a new cap profile.
    /// </summary>
    void Add(KnowledgeCapProfile profile);

    /// <summary>
    /// Persists changes to an existing cap profile.
    /// </summary>
    void Update(KnowledgeCapProfile profile);

    /// <summary>
    /// Deletes a cap profile by tag. Returns true if found and deleted.
    /// </summary>
    bool Delete(string tag);

    /// <summary>
    /// Returns true if any character progression records reference this profile tag.
    /// Used to prevent deletion of in-use profiles.
    /// </summary>
    bool IsInUse(string tag);
}
