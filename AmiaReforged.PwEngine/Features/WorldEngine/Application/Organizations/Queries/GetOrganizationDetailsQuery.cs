using AmiaReforged.PwEngine.Features.WorldEngine.Organizations;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Organizations.Queries;

/// <summary>
/// Query to get organization details
/// </summary>
public record GetOrganizationDetailsQuery : IQuery<OrganizationDetails?>
{
    public required OrganizationId OrganizationId { get; init; }
}

/// <summary>
/// DTO for organization details
/// </summary>
public record OrganizationDetails
{
    public required OrganizationId Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Type { get; init; }
    public OrganizationId? ParentOrganizationId { get; init; }
    public int MemberCount { get; init; }
    public DateTime? FoundedDate { get; init; }
}

/// <summary>
/// Handles retrieving organization details
/// </summary>
public class GetOrganizationDetailsHandler : IQueryHandler<GetOrganizationDetailsQuery, OrganizationDetails?>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IOrganizationMemberRepository _memberRepository;

    public GetOrganizationDetailsHandler(
        IOrganizationRepository organizationRepository,
        IOrganizationMemberRepository memberRepository)
    {
        _organizationRepository = organizationRepository;
        _memberRepository = memberRepository;
    }

    public Task<OrganizationDetails?> HandleAsync(GetOrganizationDetailsQuery query, CancellationToken cancellationToken = default)
    {
        var organization = _organizationRepository.GetById(query.OrganizationId);
        if (organization == null)
        {
            return Task.FromResult<OrganizationDetails?>(null);
        }

        var members = _memberRepository.GetByOrganization(query.OrganizationId);
        var activeMemberCount = members.Count(m => m.Status == SharedKernel.ValueObjects.MembershipStatus.Active);

        var details = new OrganizationDetails
        {
            Id = organization.Id,
            Name = organization.Name,
            Description = organization.Description,
            Type = organization.Type.ToString(),
            ParentOrganizationId = organization.ParentOrganization,
            MemberCount = activeMemberCount
        };

        return Task.FromResult<OrganizationDetails?>(details);
    }
}

