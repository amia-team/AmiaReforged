using AmiaReforged.PwEngine.Features.WorldEngine.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Queries;

/// <summary>
/// Query to get all recipes for an industry
/// </summary>
public record GetIndustryRecipesQuery : IQuery<List<Recipe>>
{
    public required IndustryTag IndustryTag { get; init; }
}

/// <summary>
/// Handles retrieving all recipes for an industry
/// </summary>
public class GetIndustryRecipesHandler : IQueryHandler<GetIndustryRecipesQuery, List<Recipe>>
{
    private readonly IIndustryRepository _industryRepository;

    public GetIndustryRecipesHandler(IIndustryRepository industryRepository)
    {
        _industryRepository = industryRepository;
    }

    public Task<List<Recipe>> HandleAsync(GetIndustryRecipesQuery query, CancellationToken cancellationToken = default)
    {
        Industry? industry = _industryRepository.GetByTag(query.IndustryTag);

        if (industry == null)
        {
            return Task.FromResult(new List<Recipe>());
        }

        return Task.FromResult(industry.Recipes.ToList());
    }
}

