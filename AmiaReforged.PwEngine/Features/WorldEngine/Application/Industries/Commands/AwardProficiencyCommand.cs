using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Commands;

/// <summary>
/// Command to award proficiency XP to a character's membership in an industry.
///
/// Provides an independent dispatch boundary so any activity that grants proficiency
/// XP (crafting, quest-stage rewards, ...) routes through the command dispatcher and
/// gets logging, the generic <c>CommandExecutedEvent</c>, and the domain event for free.
/// The handler loads the membership, delegates the accumulation/level-up/tier-ceiling
/// logic to <see cref="IProficiencyProgressionService"/>, persists the result, and
/// publishes <see cref="ProficiencyXpAwardedEvent"/>.
/// </summary>
public record AwardProficiencyCommand : ICommand
{
    /// <summary>
    /// Character whose industry membership receives the XP.
    /// </summary>
    public required CharacterId CharacterId { get; init; }

    /// <summary>
    /// Industry containing the membership to award XP to.
    /// </summary>
    public required IndustryTag IndustryTag { get; init; }

    /// <summary>
    /// Number of proficiency XP to award. Must be greater than zero.
    /// </summary>
    public int Points { get; init; }
}

/// <summary>
/// Handles awarding proficiency XP through the command dispatcher.
///
/// Loads the supplied membership, delegates the XP accumulation / auto-level /
/// tier-ceiling logic to <see cref="IProficiencyProgressionService"/>, persists the
/// mutated membership, and publishes <see cref="ProficiencyXpAwardedEvent"/> on
/// success. The service's domain logic is unchanged; this handler is the standalone
/// persistence/dispatch boundary the service previously lacked.
/// </summary>
[ServiceBinding(typeof(ICommandHandler<AwardProficiencyCommand>))]
public class AwardProficiencyHandler : ICommandHandler<AwardProficiencyCommand>
{
    private readonly IIndustryMembershipRepository _membershipRepository;
    private readonly IProficiencyProgressionService _proficiencyService;
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly IEventBus _eventBus;

    public AwardProficiencyHandler(
        IIndustryMembershipRepository membershipRepository,
        IProficiencyProgressionService proficiencyService,
        ICommandDispatcher commandDispatcher,
        IEventBus eventBus)
    {
        _membershipRepository = membershipRepository;
        _proficiencyService = proficiencyService;
        _commandDispatcher = commandDispatcher;
        _eventBus = eventBus;
    }

    public Task<CommandResult> HandleAsync(AwardProficiencyCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (command.Points <= 0)
            {
                return Task.FromResult(CommandResult.Fail("Cannot award zero or negative proficiency XP."));
            }

            // Load the membership for this character's membership in the industry.
            List<IndustryMembership> memberships = _membershipRepository.All(command.CharacterId.Value);
            IndustryMembership? membership =
                memberships.FirstOrDefault(m => m.IndustryTag.Value == command.IndustryTag.Value);
            if (membership is null)
            {
                return Task.FromResult(CommandResult.Fail(
                    $"Character is not a member of industry '{command.IndustryTag.Value}'"));
            }

            ProficiencyXpResult result = _proficiencyService.AwardProficiencyXp(membership, command.Points);

            // Persist the mutated membership on a successful award so the accumulated XP
            // and any level-up are durable for independent callers.
            if (result.Success)
            {
                _membershipRepository.Update(membership);
            }

            Dictionary<string, object> data = new()
            {
                ["success"] = result.Success,
                ["proficiencyXpLevel"] = result.NewLevel,
                ["proficiencyXpRemaining"] = result.XpRemaining,
                ["proficiencyXpRequired"] = result.XpRequired,
                ["proficiencyLevelsGained"] = result.LevelsGained,
                ["proficiencyAtTierCeiling"] = result.IsAtTierCeiling,
                ["message"] = result.Message ?? string.Empty
            };

            if (!result.Success)
            {
                return Task.FromResult(CommandResult.Fail(result.Message ?? string.Empty, data));
            }

            // Publish the agreed domain event.
            ProficiencyXpAwardedEvent evt = new(
                command.CharacterId,
                command.IndustryTag,
                result.NewLevel,
                result.XpRemaining,
                result.XpRequired,
                result.LevelsGained,
                result.IsAtTierCeiling,
                DateTime.UtcNow);
            _eventBus.PublishAsync(evt).GetAwaiter().GetResult();

            return Task.FromResult(CommandResult.OkWithData(data));
        }
        catch (Exception exception)
        {
            return Task.FromException<CommandResult>(exception);
        }
    }
}
