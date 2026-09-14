using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Organizations;

public interface IOrganizationRepository
{
    void Add(IOrganization organization);
    IOrganization? GetById(OrganizationId id);
    List<IOrganization> GetAll();
    List<IOrganization> GetByType(OrganizationType type);
    void Update(IOrganization organization);
    /// <summary>Deletes an organization by id. Returns true if it existed.</summary>
    bool Delete(OrganizationId id);
    void SaveChanges();
}
