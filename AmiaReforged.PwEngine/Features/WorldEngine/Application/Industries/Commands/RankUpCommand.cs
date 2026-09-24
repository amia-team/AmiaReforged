using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Commands;

/// <summary>
/// Command to advance a character one rank within an industry they belong to.
///
/// Provides an independent dispatch boundary so that any runtime entry point that ranks a
/// character up (for example <c>RuntimeCharacter.RankUp</c>) routes through the command dispatcher
/// instead of calling the membership service directly. The handler keeps only the dispatch +
/// result-mapping responsibility: all prerequisite checks, the level increment, persistence and
/// publishing of <c>ProficiencyGainedEvent</c> stay in
/// <see cref="IIndustryMembershipService.RankUp(Guid, string)"/>, which the handler delegates to.
/// </summary>
public record RankUpCommand : ICommand
{
    public required CharacterId CharacterId { get; init; }
    public required IndustryTag IndustryTag { get; init; }
}

/// <summary>
/// Handles industry rank advancement by delegating to the membership service.
///
/// The service performs the prerequisite checks (tier ceiling, knowledge points, maxed-out),
/// increments the level, persists the membership and publishes the domain event exactly once. This
/// handler maps the service's <see cref="RankUpResult"/> onto the <see cref="CommandResult"/>
/// contract so callers (such as <c>RuntimeCharacter.RankUp</c>) keep access to the outcome through
/// <see cref="CommandResult.Data"/>.
/// </summary>
[ServiceBinding(typeof(ICommandHandler<RankUpCommand>))]
public class RankUpHandler : ICommandHandler<RankUpCommand>
{
    private readonly IIndustryMembershipService _membershipService;

    public RankUpHandler(IIndustryMembershipService membershipService)
    {
        _membershipService = membershipService;
    }

    public Task<CommandResult> HandleAsync(RankUpCommand command, CancellationToken cancellationToken = default)
    {
        // Exception handling is owned by the dispatcher (CommandDispatcher.DispatchAsync wraps every
        // handler invocation and returns a generic CommandResult.Fail); nothing to do here.
        RankUpResult result = _membershipService.RankUp(command.CharacterId.Value, command.IndustryTag.Value);

        Dictionary<string, object> data = new()
        {
            ["result"] = result,
            ["success"] = result == RankUpResult.Success
        };

        return Task.FromResult(result == RankUpResult.Success
            ? CommandResult.OkWithData(data)
            : CommandResult.Fail($"Could not rank up '{command.CharacterId.Value}' in '{command.IndustryTag.Value}': {result}", data));
    }
}
