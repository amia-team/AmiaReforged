using AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Implementations;

/// <summary>
/// Concrete implementation of the Industry subsystem.
/// Thin dispatch wrapper: all operations route through the central dispatchers so
/// writes get logging, the exception-to-Fail contract, and CommandExecutedEvent publishing.
/// </summary>
[ServiceBinding(typeof(IIndustrySubsystem))]
public sealed class IndustrySubsystem : IIndustrySubsystem
{
    private readonly ICommandDispatcher _commands;
    private readonly IQueryDispatcher _queries;

    public IndustrySubsystem(
        ICommandDispatcher commands,
        IQueryDispatcher queries)
    {
        _commands = commands;
        _queries = queries;
    }

    public Task<Industry?> GetIndustryAsync(IndustryTag industryTag, CancellationToken ct = default)
    {
        GetIndustryDefinitionQuery query = new() { Tag = industryTag.Value };
        return _queries.DispatchAsync<GetIndustryDefinitionQuery, Industry?>(query, ct);
    }

    public Task<List<Industry>> GetAllIndustriesAsync(CancellationToken ct = default)
    {
        SearchIndustryDefinitionsQuery query = new() { SearchTerm = null };
        return _queries.DispatchAsync<SearchIndustryDefinitionsQuery, List<Industry>>(query, ct);
    }

    public Task<CommandResult> CraftItemAsync(CraftItemCommand command, CancellationToken ct = default)
        => _commands.DispatchAsync(command, ct);

    public Task<List<Recipe>> GetAvailableRecipesAsync(CharacterId characterId, IndustryTag industryTag, CancellationToken ct = default)
    {
        GetAvailableRecipesQuery query = new GetAvailableRecipesQuery
        {
            CharacterId = characterId,
            IndustryTag = industryTag
        };
        return _queries.DispatchAsync<GetAvailableRecipesQuery, List<Recipe>>(query, ct);
    }

    public Task<Recipe?> GetRecipeAsync(string recipeId, IndustryTag industryTag, CancellationToken ct = default)
    {
        GetRecipeQuery query = new() { IndustryTag = industryTag, RecipeId = recipeId };
        return _queries.DispatchAsync<GetRecipeQuery, Recipe?>(query, ct);
    }

    public Task<CommandResult> EnrollInIndustryAsync(CharacterId characterId, IndustryTag industryTag, CancellationToken ct = default)
    {
        EnrollInIndustryCommand command = new() { CharacterId = characterId, IndustryTag = industryTag };
        return _commands.DispatchAsync(command, ct);
    }

    public Task<IndustryMembership?> GetMembershipAsync(CharacterId characterId, IndustryTag industryTag, CancellationToken ct = default)
    {
        GetMembershipQuery query = new() { CharacterId = characterId, IndustryTag = industryTag };
        return _queries.DispatchAsync<GetMembershipQuery, IndustryMembership?>(query, ct);
    }

    public Task<List<IndustryMembership>> GetCharacterIndustriesAsync(CharacterId characterId, CancellationToken ct = default)
    {
        GetCharacterIndustriesQuery query = new() { CharacterId = characterId };
        return _queries.DispatchAsync<GetCharacterIndustriesQuery, List<IndustryMembership>>(query, ct);
    }

    public Task<CommandResult> LearnRecipeAsync(CharacterId characterId, IndustryTag industryTag, string recipeId, CancellationToken ct = default)
    {
        LearnRecipeCommand command = new() { CharacterId = characterId, IndustryTag = industryTag, RecipeId = recipeId };
        return _commands.DispatchAsync(command, ct);
    }

    public Task<List<string>> GetKnownRecipesAsync(CharacterId characterId, IndustryTag industryTag, CancellationToken ct = default)
    {
        GetKnownRecipesQuery query = new() { CharacterId = characterId, IndustryTag = industryTag };
        return _queries.DispatchAsync<GetKnownRecipesQuery, List<string>>(query, ct);
    }

    public Task<CommandResult> AddRecipeToIndustryAsync(AddRecipeToIndustryCommand command, CancellationToken ct = default)
        => _commands.DispatchAsync(command, ct);

    public Task<CommandResult> RemoveRecipeFromIndustryAsync(RemoveRecipeFromIndustryCommand command, CancellationToken ct = default)
        => _commands.DispatchAsync(command, ct);

    public Task<List<Recipe>> GetWorkstationRecipesAsync(
        CharacterId characterId, WorkstationTag workstationTag, CancellationToken ct = default)
    {
        GetWorkstationRecipesQuery query = new GetWorkstationRecipesQuery
        {
            CharacterId = characterId,
            WorkstationTag = workstationTag
        };
        return _queries.DispatchAsync<GetWorkstationRecipesQuery, List<Recipe>>(query, ct);
    }
}
