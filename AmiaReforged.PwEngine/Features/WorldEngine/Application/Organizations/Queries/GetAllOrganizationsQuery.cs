using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Organizations;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Organizations.Queries;

/// <summary>
/// Query to list all organizations. Callers apply type/search/pagination filters.
/// </summary>
public record GetAllOrganizationsQuery : IQuery<List<IOrganization>>;

/// <summary>
/// Handles listing all organizations.
/// </summary>
[ServiceBinding(typeof(IQueryHandler<GetAllOrganizationsQuery, List<IOrganization>>))]
public class GetAllOrganizationsHandler : IQueryHandler<GetAllOrganizationsQuery, List<IOrganization>>
{
    private readonly IOrganizationRepository _organizationRepository;

    public GetAllOrganizationsHandler(IOrganizationRepository organizationRepository)
    {
        _organizationRepository = organizationRepository;
    }

    public Task<List<IOrganization>> HandleAsync(
        GetAllOrganizationsQuery query,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_organizationRepository.GetAll());
    }
}
