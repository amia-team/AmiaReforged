using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Organizations;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Tests.Systems.WorldEngine.Helpers;

public class InMemoryOrganizationRepository : IOrganizationRepository
{
    private readonly Dictionary<OrganizationId, IOrganization> _organizations = new();

    public void Add(IOrganization organization)
    {
        _organizations[organization.Id] = organization;
    }

    public IOrganization? GetById(OrganizationId organizationId)
    {
        return _organizations.GetValueOrDefault(organizationId);
    }

    public List<IOrganization> GetAll()
    {
        return _organizations.Values.ToList();
    }

    public List<IOrganization> GetByType(OrganizationType type)
    {
        return _organizations.Values.Where(o => o.Type == type).ToList();
    }

    public void Update(IOrganization organization)
    {
        if (_organizations.ContainsKey(organization.Id))
        {
            _organizations[organization.Id] = organization;
        }
    }

    public void SaveChanges()
    {
        // No-op for in-memory implementation
    }
}
