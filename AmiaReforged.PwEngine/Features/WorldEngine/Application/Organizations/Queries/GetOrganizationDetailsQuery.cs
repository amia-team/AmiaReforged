using AmiaReforged.PwEngine.Features.WorldEngine.Organizations;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Organizations.Queries;

/// <summary>
/// Query to get organization details
/// </summary>
public record GetOrganizationDetailsQuery : IQuery<IOrganization?>
{
    public required OrganizationId OrganizationId { get; init; }
}

/// <summary>
/// Handles retrieving organization details
/// </summary>
[ServiceBinding(typeof(IQueryHandler<GetOrganizationDetailsQuery, IOrganization?>))]
public class GetOrganizationDetailsHandler : IQueryHandler<GetOrganizationDetailsQuery, IOrganization?>
{
    private readonly IOrganizationRepository _organizationRepository;

    public GetOrganizationDetailsHandler(IOrganizationRepository organizationRepository)
    {
        _organizationRepository = organizationRepository;
    }

    public Task<IOrganization?> HandleAsync(
        GetOrganizationDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        IOrganization? organization = _organizationRepository.GetById(query.OrganizationId);
        return Task.FromResult(organization);
    }
}
