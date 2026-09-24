using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Commands;

/// <summary>
/// Command to enroll a character in an industry.
/// </summary>
public record EnrollInIndustryCommand : ICommand
{
    public required CharacterId CharacterId { get; init; }
    public required IndustryTag IndustryTag { get; init; }
}

/// <summary>
/// Handles enrolling characters in industries.
/// </summary>
[ServiceBinding(typeof(ICommandHandler<EnrollInIndustryCommand>))]
public class EnrollInIndustryHandler : ICommandHandler<EnrollInIndustryCommand>
{
    private readonly IIndustryRepository _industryRepository;
    private readonly IIndustryMembershipRepository _membershipRepository;
    private readonly ICharacterRepository _characterRepository;
    private readonly IEventBus _eventBus;

    public EnrollInIndustryHandler(
        IIndustryRepository industryRepository,
        IIndustryMembershipRepository membershipRepository,
        ICharacterRepository characterRepository,
        IEventBus eventBus)
    {
        _industryRepository = industryRepository;
        _membershipRepository = membershipRepository;
        _characterRepository = characterRepository;
        _eventBus = eventBus;
    }

    public Task<CommandResult> HandleAsync(EnrollInIndustryCommand command, CancellationToken cancellationToken = default)
    {
        Industry? industry = _industryRepository.GetByTag(command.IndustryTag);
        if (industry == null)
        {
            return Task.FromResult(CommandResult.Fail($"Industry '{command.IndustryTag.Value}' not found"));
        }

        if (!_characterRepository.Exists(command.CharacterId.Value))
        {
            return Task.FromResult(CommandResult.Fail($"Character '{command.CharacterId.Value}' not found"));
        }

        List<IndustryMembership> existing = _membershipRepository.All(command.CharacterId.Value);
        if (existing.Any(m => m.IndustryTag.Value == command.IndustryTag.Value))
        {
            return Task.FromResult(CommandResult.Fail($"Already enrolled in '{command.IndustryTag.Value}'"));
        }

        IndustryMembership membership = new()
        {
            CharacterId = command.CharacterId,
            IndustryTag = command.IndustryTag,
            Level = ProficiencyLevel.Novice,
            CharacterKnowledge = []
        };
        _membershipRepository.Add(membership);
        _membershipRepository.SaveChanges();

        // Publish event
        MemberJoinedIndustryEvent evt = new(
            membership.CharacterId,
            membership.IndustryTag,
            membership.Level,
            DateTime.UtcNow);
        _eventBus.PublishAsync(evt).GetAwaiter().GetResult();

        return Task.FromResult(CommandResult.Ok());
    }
}
