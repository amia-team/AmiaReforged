using AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;

/// <summary>
/// Provides access to industry-related operations including crafting, recipes, and skill progression.
/// </summary>
public interface IIndustrySubsystem
{
    // === Industry Management ===

    /// <summary>
    /// Gets an industry by its tag.
    /// </summary>
    Task<Industry?> GetIndustryAsync(IndustryTag industryTag, CancellationToken ct = default);

    /// <summary>
    /// Gets all available industries.
    /// </summary>
    Task<List<Industry>> GetAllIndustriesAsync(CancellationToken ct = default);

    // === Crafting Operations ===

    /// <summary>
    /// Attempts to craft an item using a recipe.
    /// </summary>
    Task<CommandResult> CraftItemAsync(CraftItemCommand command, CancellationToken ct = default);

    /// <summary>
    /// Gets available recipes for a character in an industry.
    /// </summary>
    Task<List<Recipe>> GetAvailableRecipesAsync(
        CharacterId characterId, IndustryTag industryTag, CancellationToken ct = default);

    /// <summary>
    /// Gets a specific recipe by ID.
    /// </summary>
    Task<Recipe?> GetRecipeAsync(string recipeId, IndustryTag industryTag, CancellationToken ct = default);

    // === Industry Membership ===

    /// <summary>
    /// Enrolls a character in an industry.
    /// </summary>
    Task<CommandResult> EnrollInIndustryAsync(CharacterId characterId, IndustryTag industryTag, CancellationToken ct = default);

    /// <summary>
    /// Gets a character's membership in an industry.
    /// </summary>
    Task<IndustryMembership?> GetMembershipAsync(
        CharacterId characterId, IndustryTag industryTag, CancellationToken ct = default);

    /// <summary>
    /// Gets all industries a character is enrolled in.
    /// </summary>
    Task<List<IndustryMembership>> GetCharacterIndustriesAsync(
        CharacterId characterId, CancellationToken ct = default);

    // === Recipe Learning ===

    /// <summary>
    /// Teaches a recipe to a character.
    /// </summary>
    Task<CommandResult> LearnRecipeAsync(CharacterId characterId, IndustryTag industryTag, string recipeId, CancellationToken ct = default);

    /// <summary>
    /// Gets all recipes known by a character in an industry.
    /// </summary>
    Task<List<string>> GetKnownRecipesAsync(
        CharacterId characterId, IndustryTag industryTag, CancellationToken ct = default);

    // === Industry Configuration ===

    /// <summary>
    /// Adds a recipe to an industry definition.
    /// </summary>
    Task<CommandResult> AddRecipeToIndustryAsync(AddRecipeToIndustryCommand command, CancellationToken ct = default);

    /// <summary>
    /// Removes a recipe from an industry definition.
    /// </summary>
    Task<CommandResult> RemoveRecipeFromIndustryAsync(
        RemoveRecipeFromIndustryCommand command, CancellationToken ct = default);
}

