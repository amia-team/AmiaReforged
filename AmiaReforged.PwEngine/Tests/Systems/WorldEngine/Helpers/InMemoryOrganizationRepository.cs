using AmiaReforged.PwEngine.Systems.WorldEngine.Organizations;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Helpers;

public class InMemoryOrganizationRepository : IOrganizationRepository
{
    private readonly Dictionary<OrganizationId, IOrganization> _organizations = new();

    public void Create(IOrganization organization)
    {
        _organizations.TryAdd(organization.Id, organization);
    }

    public void Update(IOrganization organization)
    {
        if (_organizations.ContainsKey(organization.Id))
        {
            _organizations[organization.Id] = organization;
        }
    }

    public void Delete(IOrganization organization)
    {
        _organizations.Remove(organization.Id);
    }

    public IOrganization? GetById(OrganizationId organizationId)
    {
        return _organizations.GetValueOrDefault(organizationId);
    }

    public List<IOrganization> All()
    {
        return _organizations.Values.ToList();
    }

    public void SaveChanges()
    {
        // No-op for in-memory implementation
    }
}
