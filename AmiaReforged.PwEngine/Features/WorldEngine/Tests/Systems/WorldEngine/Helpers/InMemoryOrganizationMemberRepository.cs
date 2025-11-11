using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Organizations;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Tests.Systems.WorldEngine.Helpers;

/// <summary>
/// In-memory implementation of IOrganizationMemberRepository for testing
/// </summary>
public class InMemoryOrganizationMemberRepository : IOrganizationMemberRepository
{
    private readonly List<OrganizationMember> _members = [];

    public void Add(OrganizationMember member)
    {
        _members.Add(member);
    }

    public OrganizationMember? GetByCharacterAndOrganization(CharacterId characterId, OrganizationId organizationId)
    {
        return _members.FirstOrDefault(m =>
            m.CharacterId.Equals(characterId) &&
            m.OrganizationId.Equals(organizationId));
    }

    public List<OrganizationMember> GetByOrganization(OrganizationId organizationId)
    {
        return _members.Where(m => m.OrganizationId.Equals(organizationId)).ToList();
    }

    public List<OrganizationMember> GetByCharacter(CharacterId characterId)
    {
        return _members.Where(m => m.CharacterId.Equals(characterId)).ToList();
    }

    public void Update(OrganizationMember member)
    {
        OrganizationMember? existing = _members.FirstOrDefault(m => m.Id == member.Id);
        if (existing != null)
        {
            _members.Remove(existing);
            _members.Add(member);
        }
    }

    public void Remove(OrganizationMember member)
    {
        _members.Remove(member);
    }

    public void SaveChanges()
    {
        // No-op for in-memory implementation
    }
}

