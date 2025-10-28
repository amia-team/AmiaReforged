using System.Collections.Generic;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Organizations;

public interface IOrganizationMemberRepository
{
    void Add(OrganizationMember member);
    OrganizationMember? GetByCharacterAndOrganization(CharacterId characterId, OrganizationId organizationId);
    List<OrganizationMember> GetByOrganization(OrganizationId organizationId);
    List<OrganizationMember> GetByCharacter(CharacterId characterId);
    void Update(OrganizationMember member);
    void Remove(OrganizationMember member);
    void SaveChanges();
}
