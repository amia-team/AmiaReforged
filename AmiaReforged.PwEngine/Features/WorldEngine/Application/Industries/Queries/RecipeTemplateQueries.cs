using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Queries;

/// <summary>
/// Gets a single recipe template by tag.
/// </summary>
public record GetRecipeTemplateQuery : IQuery<RecipeTemplate?>
{
    public required string Tag { get; init; }
}

/// <summary>
/// Searches recipe templates by name/tag. Empty term returns all.
/// </summary>
public record SearchRecipeTemplatesQuery : IQuery<List<RecipeTemplate>>
{
    public string? SearchTerm { get; init; }
}

/// <summary>
/// Lists recipe templates for one industry.
/// </summary>
public record GetRecipeTemplatesByIndustryQuery : IQuery<List<RecipeTemplate>>
{
    public required IndustryTag IndustryTag { get; init; }
}

[ServiceBinding(typeof(IQueryHandler<GetRecipeTemplateQuery, RecipeTemplate?>))]
public sealed class GetRecipeTemplateHandler : IQueryHandler<GetRecipeTemplateQuery, RecipeTemplate?>
{
    private readonly IRecipeTemplateRepository _repository;

    public GetRecipeTemplateHandler(IRecipeTemplateRepository repository)
    {
        _repository = repository;
    }

    public Task<RecipeTemplate?> HandleAsync(GetRecipeTemplateQuery query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_repository.GetByTag(query.Tag));
    }
}

[ServiceBinding(typeof(IQueryHandler<SearchRecipeTemplatesQuery, List<RecipeTemplate>>))]
public sealed class SearchRecipeTemplatesHandler : IQueryHandler<SearchRecipeTemplatesQuery, List<RecipeTemplate>>
{
    private readonly IRecipeTemplateRepository _repository;

    public SearchRecipeTemplatesHandler(IRecipeTemplateRepository repository)
    {
        _repository = repository;
    }

    public Task<List<RecipeTemplate>> HandleAsync(SearchRecipeTemplatesQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.SearchTerm))
            return Task.FromResult(_repository.All());

        string term = query.SearchTerm.Trim();
        return Task.FromResult(_repository.All()
            .Where(t => t.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                        || t.Tag.Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList());
    }
}

[ServiceBinding(typeof(IQueryHandler<GetRecipeTemplatesByIndustryQuery, List<RecipeTemplate>>))]
public sealed class GetRecipeTemplatesByIndustryHandler : IQueryHandler<GetRecipeTemplatesByIndustryQuery, List<RecipeTemplate>>
{
    private readonly IRecipeTemplateRepository _repository;

    public GetRecipeTemplatesByIndustryHandler(IRecipeTemplateRepository repository)
    {
        _repository = repository;
    }

    public Task<List<RecipeTemplate>> HandleAsync(GetRecipeTemplatesByIndustryQuery query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_repository.GetByIndustry(query.IndustryTag));
    }
}
