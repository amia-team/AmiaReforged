using AmiaReforged.PwEngine.Features.WorldEngine.Organizations;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Organizations.Queries;

/// <summary>
/// Query to get all organizations a character belongs to
/// </summary>
public record GetCharacterOrganizationsQuery : IQuery<List<OrganizationMember>>
{
    public required CharacterId CharacterId { get; init; }
    public bool ActiveOnly { get; init; } = true;
}

/// <summary>
/// Handles retrieving character's organization memberships
/// </summary>
public class GetCharacterOrganizationsHandler : IQueryHandler<GetCharacterOrganizationsQuery, List<OrganizationMember>>
{
    private readonly IOrganizationMemberRepository _memberRepository;
    private readonly IOrganizationRepository _organizationRepository;

    public GetCharacterOrganizationsHandler(
        IOrganizationMemberRepository memberRepository,
        IOrganizationRepository organizationRepository)
    {
        _memberRepository = memberRepository;
        _organizationRepository = organizationRepository;
    }

    public Task<List<OrganizationMember>> HandleAsync(
        GetCharacterOrganizationsQuery query,
        CancellationToken cancellationToken = default)
    {
        var memberships = _memberRepository.GetByCharacter(query.CharacterId);

        if (query.ActiveOnly)
        {
            memberships = memberships.Where(m => m.Status == MembershipStatus.Active).ToList();
        }

        return Task.FromResult(memberships);
    }
}

