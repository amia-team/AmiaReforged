using AmiaReforged.PwEngine.Features.WorldEngine.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Commands;

/// <summary>
/// Command to add a recipe to an industry
/// </summary>
public record AddRecipeToIndustryCommand : ICommand
{
    public required IndustryTag IndustryTag { get; init; }
    public required Recipe Recipe { get; init; }
}

/// <summary>
/// Handles adding recipes to industries
/// </summary>
public class AddRecipeToIndustryHandler : ICommandHandler<AddRecipeToIndustryCommand>
{
    private readonly IIndustryRepository _industryRepository;

    public AddRecipeToIndustryHandler(IIndustryRepository industryRepository)
    {
        _industryRepository = industryRepository;
    }

    public Task<CommandResult> HandleAsync(AddRecipeToIndustryCommand command, CancellationToken cancellationToken = default)
    {
        var industry = _industryRepository.GetByTag(command.IndustryTag);

        if (industry == null)
        {
            return Task.FromResult(CommandResult.Fail($"Industry '{command.IndustryTag.Value}' not found"));
        }

        // Check if recipe already exists
        if (industry.Recipes.Any(r => r.RecipeId == command.Recipe.RecipeId))
        {
            return Task.FromResult(CommandResult.Fail($"Recipe '{command.Recipe.RecipeId.Value}' already exists in industry '{command.IndustryTag.Value}'"));
        }

        // Validate that recipe belongs to this industry
        if (command.Recipe.IndustryTag != command.IndustryTag)
        {
            return Task.FromResult(CommandResult.Fail($"Recipe industry tag '{command.Recipe.IndustryTag.Value}' does not match target industry '{command.IndustryTag.Value}'"));
        }

        industry.Recipes.Add(command.Recipe);

        return Task.FromResult(CommandResult.Ok());
    }
}

