using System.Collections.Generic;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Organizations;

public interface IOrganizationRepository
{
    void Add(IOrganization organization);
    IOrganization? GetById(OrganizationId id);
    List<IOrganization> GetAll();
    List<IOrganization> GetByType(OrganizationType type);
    void Update(IOrganization organization);
    void SaveChanges();
}
