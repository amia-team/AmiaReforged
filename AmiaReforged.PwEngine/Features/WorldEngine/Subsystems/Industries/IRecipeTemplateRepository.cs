using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

/// <summary>
/// Repository interface for recipe template persistence operations.
/// </summary>
public interface IRecipeTemplateRepository
{
    /// <summary>
    /// Adds a new recipe template, or updates it if one with the same tag already exists (upsert).
    /// </summary>
    void Add(RecipeTemplate template);

    /// <summary>
    /// Gets a recipe template by its unique tag.
    /// </summary>
    RecipeTemplate? GetByTag(string tag);

    /// <summary>
    /// Gets all recipe templates belonging to a specific industry.
    /// </summary>
    List<RecipeTemplate> GetByIndustry(IndustryTag industryTag);

    /// <summary>
    /// Gets all recipe templates.
    /// </summary>
    List<RecipeTemplate> All();

    /// <summary>
    /// Updates an existing recipe template.
    /// </summary>
    void Update(RecipeTemplate template);

    /// <summary>
    /// Deletes a recipe template by tag. Returns true if found and deleted.
    /// </summary>
    bool Delete(string tag);

    /// <summary>
    /// Searches recipe templates with optional text filter and pagination.
    /// </summary>
    List<RecipeTemplate> Search(string? searchTerm, int page, int pageSize, out int totalCount);
}
