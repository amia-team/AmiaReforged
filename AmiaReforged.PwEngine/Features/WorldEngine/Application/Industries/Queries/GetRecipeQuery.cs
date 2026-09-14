using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Queries;

/// <summary>
/// Query to get a single recipe by ID within an industry.
/// Includes template-expanded recipes (manual definitions take precedence on
/// ID collision), matching the lookup order in <c>CraftItemHandler</c> so
/// single-recipe reads and the craft path agree.
/// </summary>
public record GetRecipeQuery : IQuery<Recipe?>
{
    public required IndustryTag IndustryTag { get; init; }
    public required string RecipeId { get; init; }
}

/// <summary>
/// Handles retrieving a single recipe, including template-expanded ones.
/// </summary>
[ServiceBinding(typeof(IQueryHandler<GetRecipeQuery, Recipe?>))]
public class GetRecipeHandler : IQueryHandler<GetRecipeQuery, Recipe?>
{
    private readonly IIndustryRepository _industryRepository;
    private readonly RecipeTemplateExpander _templateExpander;

    public GetRecipeHandler(
        IIndustryRepository industryRepository,
        RecipeTemplateExpander templateExpander)
    {
        _industryRepository = industryRepository;
        _templateExpander = templateExpander;
    }

    public Task<Recipe?> HandleAsync(GetRecipeQuery query, CancellationToken cancellationToken = default)
    {
        Industry? industry = _industryRepository.GetByTag(query.IndustryTag);
        if (industry == null)
        {
            return Task.FromResult<Recipe?>(null);
        }

        Recipe? recipe = industry.Recipes.FirstOrDefault(r => r.RecipeId.Value == query.RecipeId)
            ?? _templateExpander.GetExpandedRecipes(query.IndustryTag)
                .FirstOrDefault(r => r.RecipeId.Value == query.RecipeId);
        return Task.FromResult(recipe);
    }
}
