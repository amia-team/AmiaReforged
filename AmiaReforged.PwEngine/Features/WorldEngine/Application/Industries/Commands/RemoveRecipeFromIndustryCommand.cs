using AmiaReforged.PwEngine.Features.WorldEngine.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Commands;

/// <summary>
/// Command to remove a recipe from an industry
/// </summary>
public record RemoveRecipeFromIndustryCommand : ICommand
{
    public required IndustryTag IndustryTag { get; init; }
    public required RecipeId RecipeId { get; init; }
}

/// <summary>
/// Handles removing recipes from industries
/// </summary>
public class RemoveRecipeFromIndustryHandler : ICommandHandler<RemoveRecipeFromIndustryCommand>
{
    private readonly IIndustryRepository _industryRepository;

    public RemoveRecipeFromIndustryHandler(IIndustryRepository industryRepository)
    {
        _industryRepository = industryRepository;
    }

    public Task<CommandResult> HandleAsync(RemoveRecipeFromIndustryCommand command, CancellationToken cancellationToken = default)
    {
        Industry? industry = _industryRepository.GetByTag(command.IndustryTag);

        if (industry == null)
        {
            return Task.FromResult(CommandResult.Fail($"Industry '{command.IndustryTag.Value}' not found"));
        }

        Recipe? recipe = industry.Recipes.FirstOrDefault(r => r.RecipeId == command.RecipeId);

        if (recipe == null)
        {
            return Task.FromResult(CommandResult.Fail($"Recipe '{command.RecipeId.Value}' not found in industry '{command.IndustryTag.Value}'"));
        }

        industry.Recipes.Remove(recipe);

        return Task.FromResult(CommandResult.Ok());
    }
}

