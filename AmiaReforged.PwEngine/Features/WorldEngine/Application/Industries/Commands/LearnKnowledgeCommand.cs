using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Commands;

/// <summary>
/// Command to learn a piece of knowledge for a character.
///
/// Provides an independent dispatch boundary so that any runtime entry point that teaches
/// knowledge (for example <c>RuntimeCharacter.Learn</c>) routes through the command dispatcher
/// instead of calling the service directly. The handler keeps only the dispatch + result-mapping
/// responsibility: all prerequisite checks, point deduction, persistence of
/// <c>CharacterKnowledge</c> and publishing of <c>RecipeLearnedEvent</c> stay in
/// <see cref="IIndustryMembershipService.LearnKnowledge"/>, which the handler delegates to.
///
/// Recipe availability validation is intentionally separate (see
/// <see cref="LearnRecipeCommand"/>): recipes carry no learnable state of their own, while this
/// command persists actual knowledge state. Prerequisite checks (rank, points, not-already-known)
/// are preserved inside the delegated service call.
/// </summary>
public record LearnKnowledgeCommand : ICommand
{
    public required CharacterId CharacterId { get; init; }
    public required string KnowledgeTag { get; init; }
}

/// <summary>
/// Handles knowledge learning by delegating to the membership service.
///
/// The service performs the prerequisite checks, deducts knowledge points, persists the learned
/// knowledge and publishes the domain event exactly once. This handler maps the service's
/// <see cref="LearningResult"/> onto the <see cref="CommandResult"/> contract so callers (such as
/// <c>RuntimeCharacter.Learn</c>) keep access to the outcome through <see cref="CommandResult.Data"/>.
/// </summary>
[ServiceBinding(typeof(ICommandHandler<LearnKnowledgeCommand>))]
public class LearnKnowledgeHandler : ICommandHandler<LearnKnowledgeCommand>
{
    private readonly IIndustryMembershipService _membershipService;

    public LearnKnowledgeHandler(IIndustryMembershipService membershipService)
    {
        _membershipService = membershipService;
    }

    public Task<CommandResult> HandleAsync(LearnKnowledgeCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            LearningResult result = _membershipService.LearnKnowledge(command.CharacterId, command.KnowledgeTag);

            Dictionary<string, object> data = new()
            {
                ["result"] = result,
                ["success"] = result == LearningResult.Success
            };

            if (result == LearningResult.Success)
            {
                return Task.FromResult(CommandResult.OkWithData(data));
            }

            return Task.FromResult(CommandResult.Fail($"Could not learn knowledge '{command.KnowledgeTag}': {result}", data));
        }
        catch (Exception exception)
        {
            return Task.FromException<CommandResult>(exception);
        }
    }
}
