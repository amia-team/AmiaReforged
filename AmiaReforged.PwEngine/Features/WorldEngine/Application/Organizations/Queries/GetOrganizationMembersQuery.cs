using AmiaReforged.PwEngine.Features.WorldEngine.Organizations;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Organizations.Queries;

/// <summary>
/// Query to get all members of an organization
/// </summary>
public record GetOrganizationMembersQuery : IQuery<List<OrganizationMember>>
{
    public required OrganizationId OrganizationId { get; init; }
    public bool ActiveOnly { get; init; } = true;
}

/// <summary>
/// Handles retrieving organization members
/// </summary>
public class GetOrganizationMembersHandler : IQueryHandler<GetOrganizationMembersQuery, List<OrganizationMember>>
{
    private readonly IOrganizationMemberRepository _memberRepository;

    public GetOrganizationMembersHandler(IOrganizationMemberRepository memberRepository)
    {
        _memberRepository = memberRepository;
    }

    public Task<List<OrganizationMember>> HandleAsync(
        GetOrganizationMembersQuery query,
        CancellationToken cancellationToken = default)
    {
        List<OrganizationMember> members = _memberRepository.GetByOrganization(query.OrganizationId);

        if (query.ActiveOnly)
        {
            members = members.Where(m => m.Status == MembershipStatus.Active).ToList();
        }

        return Task.FromResult(members);
    }
}

