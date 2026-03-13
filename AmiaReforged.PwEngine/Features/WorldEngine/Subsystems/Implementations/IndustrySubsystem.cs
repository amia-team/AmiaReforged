using AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Implementations;

/// <summary>
/// Concrete implementation of the Industry subsystem.
/// Delegates to existing command and query handlers.
/// </summary>
[ServiceBinding(typeof(IIndustrySubsystem))]
public sealed class IndustrySubsystem : IIndustrySubsystem
{
    private readonly IIndustryRepository _industryRepository;
    private readonly IIndustryMembershipRepository _membershipRepository;
    private readonly ICharacterKnowledgeRepository _knowledgeRepository;
    private readonly ICommandHandler<CraftItemCommand> _craftHandler;
    private readonly ICommandHandler<AddRecipeToIndustryCommand> _addRecipeHandler;
    private readonly ICommandHandler<RemoveRecipeFromIndustryCommand> _removeRecipeHandler;
    private readonly IQueryHandler<GetAvailableRecipesQuery, List<Recipe>> _availableRecipesHandler;
    private readonly IQueryHandler<GetWorkstationRecipesQuery, List<Recipe>> _workstationRecipesHandler;

    public IndustrySubsystem(
        IIndustryRepository industryRepository,
        IIndustryMembershipRepository membershipRepository,
        ICharacterKnowledgeRepository knowledgeRepository,
        ICommandHandler<CraftItemCommand> craftHandler,
        ICommandHandler<AddRecipeToIndustryCommand> addRecipeHandler,
        ICommandHandler<RemoveRecipeFromIndustryCommand> removeRecipeHandler,
        IQueryHandler<GetAvailableRecipesQuery, List<Recipe>> availableRecipesHandler,
        IQueryHandler<GetWorkstationRecipesQuery, List<Recipe>> workstationRecipesHandler)
    {
        _industryRepository = industryRepository;
        _membershipRepository = membershipRepository;
        _knowledgeRepository = knowledgeRepository;
        _craftHandler = craftHandler;
        _addRecipeHandler = addRecipeHandler;
        _removeRecipeHandler = removeRecipeHandler;
        _availableRecipesHandler = availableRecipesHandler;
        _workstationRecipesHandler = workstationRecipesHandler;
    }

    public Task<Industry?> GetIndustryAsync(IndustryTag industryTag, CancellationToken ct = default)
    {
        Industry? industry = _industryRepository.GetByTag(industryTag);
        return Task.FromResult(industry);
    }

    public Task<List<Industry>> GetAllIndustriesAsync(CancellationToken ct = default)
    {
        List<Industry> all = _industryRepository.All();
        return Task.FromResult(all);
    }

    public Task<CommandResult> CraftItemAsync(CraftItemCommand command, CancellationToken ct = default)
        => _craftHandler.HandleAsync(command, ct);

    public Task<List<Recipe>> GetAvailableRecipesAsync(CharacterId characterId, IndustryTag industryTag, CancellationToken ct = default)
    {
        GetAvailableRecipesQuery query = new GetAvailableRecipesQuery
        {
            CharacterId = characterId,
            IndustryTag = industryTag
        };
        return _availableRecipesHandler.HandleAsync(query, ct);
    }

    public Task<Recipe?> GetRecipeAsync(string recipeId, IndustryTag industryTag, CancellationToken ct = default)
    {
        Industry? industry = _industryRepository.GetByTag(industryTag);
        Recipe? recipe = industry?.Recipes.FirstOrDefault(r => r.RecipeId.Value == recipeId);
        return Task.FromResult(recipe);
    }

    public Task<CommandResult> EnrollInIndustryAsync(CharacterId characterId, IndustryTag industryTag, CancellationToken ct = default)
    {
        Industry? industry = _industryRepository.GetByTag(industryTag);
        if (industry == null)
            return Task.FromResult(CommandResult.Fail($"Industry '{industryTag.Value}' not found"));

        List<IndustryMembership> existing = _membershipRepository.All(characterId.Value);
        if (existing.Any(m => m.IndustryTag.Value == industryTag.Value))
            return Task.FromResult(CommandResult.Fail($"Already enrolled in '{industryTag.Value}'"));

        IndustryMembership membership = new IndustryMembership
        {
            CharacterId = characterId,
            IndustryTag = industryTag,
            Level = ProficiencyLevel.Layman,
            CharacterKnowledge = []
        };
        _membershipRepository.Add(membership);
        _membershipRepository.SaveChanges();
        return Task.FromResult(CommandResult.Ok());
    }

    public Task<IndustryMembership?> GetMembershipAsync(CharacterId characterId, IndustryTag industryTag, CancellationToken ct = default)
    {
        List<IndustryMembership> memberships = _membershipRepository.All(characterId.Value);
        IndustryMembership? membership = memberships.FirstOrDefault(m => m.IndustryTag.Value == industryTag.Value);
        return Task.FromResult(membership);
    }

    public Task<List<IndustryMembership>> GetCharacterIndustriesAsync(CharacterId characterId, CancellationToken ct = default)
    {
        List<IndustryMembership> memberships = _membershipRepository.All(characterId.Value);
        return Task.FromResult(memberships);
    }

    public Task<CommandResult> LearnRecipeAsync(CharacterId characterId, IndustryTag industryTag, string recipeId, CancellationToken ct = default)
    {
        // Verify the recipe exists
        Industry? industry = _industryRepository.GetByTag(industryTag);
        Recipe? recipe = industry?.Recipes.FirstOrDefault(r => r.RecipeId.Value == recipeId);
        if (recipe == null)
            return Task.FromResult(CommandResult.Fail($"Recipe '{recipeId}' not found in industry '{industryTag.Value}'"));

        // Check if character has the required knowledge prereqs
        List<Knowledge> known = _knowledgeRepository.GetAllKnowledge(characterId.Value);
        HashSet<string> knownTags = known.Select(k => k.Tag).ToHashSet();
        if (!recipe.RequiredKnowledge.All(req => knownTags.Contains(req)))
            return Task.FromResult(CommandResult.Fail("Missing prerequisite knowledge"));

        return Task.FromResult(CommandResult.Ok());
    }

    public Task<List<string>> GetKnownRecipesAsync(CharacterId characterId, IndustryTag industryTag, CancellationToken ct = default)
    {
        List<CharacterKnowledge> knowledge = _knowledgeRepository.GetKnowledgeForIndustry(industryTag.Value, characterId.Value);
        List<string> knownTags = knowledge.Select(k => k.Definition.Tag).ToList();
        return Task.FromResult(knownTags);
    }

    public Task<CommandResult> AddRecipeToIndustryAsync(AddRecipeToIndustryCommand command, CancellationToken ct = default)
        => _addRecipeHandler.HandleAsync(command, ct);

    public Task<CommandResult> RemoveRecipeFromIndustryAsync(RemoveRecipeFromIndustryCommand command, CancellationToken ct = default)
        => _removeRecipeHandler.HandleAsync(command, ct);

    public Task<List<Recipe>> GetWorkstationRecipesAsync(
        CharacterId characterId, WorkstationTag workstationTag, CancellationToken ct = default)
    {
        GetWorkstationRecipesQuery query = new GetWorkstationRecipesQuery
        {
            CharacterId = characterId,
            WorkstationTag = workstationTag
        };
        return _workstationRecipesHandler.HandleAsync(query, ct);
    }
}

