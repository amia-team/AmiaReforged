namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions;

/// <summary>
/// Repository for persisting and retrieving <see cref="InteractionDefinition"/> objects.
/// Follows the same convention as <see cref="ResourceNodes.IResourceNodeDefinitionRepository"/>
/// and <see cref="Industries.IIndustryRepository"/>.
/// </summary>
public interface IInteractionDefinitionRepository
{
    /// <summary>Returns the definition with the given <paramref name="tag"/>, or <c>null</c>.</summary>
    InteractionDefinition? Get(string tag);

    /// <summary>Returns all stored definitions.</summary>
    List<InteractionDefinition> All();

    /// <summary>Returns <c>true</c> if a definition with <paramref name="tag"/> exists.</summary>
    bool Exists(string tag);

    /// <summary>
    /// Creates or upserts a definition.
    /// If a definition with the same tag already exists, it is replaced.
    /// </summary>
    void Create(InteractionDefinition definition);

    /// <summary>Updates an existing definition. No-op if the tag doesn't exist.</summary>
    void Update(InteractionDefinition definition);

    /// <summary>Deletes a definition by tag. Returns <c>true</c> if it existed.</summary>
    bool Delete(string tag);

    /// <summary>
    /// Searches definitions by name or tag with pagination.
    /// </summary>
    List<InteractionDefinition> Search(string? search, int page, int pageSize, out int totalCount);
}
