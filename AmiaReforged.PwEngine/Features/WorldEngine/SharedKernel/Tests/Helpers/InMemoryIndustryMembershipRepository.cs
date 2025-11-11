using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Helpers;

public class InMemoryIndustryMembershipRepository : IIndustryMembershipRepository
{
    private readonly List<IndustryMembership> _memberships = [];

    public void Add(IndustryMembership membership)
    {
        _memberships.Add(membership);
    }

    public void Update(IndustryMembership membership)
    {
        _memberships.Remove(membership);
        _memberships.Add(membership);
    }

    public List<IndustryMembership> All(Guid characterGuid)
    {
        return _memberships.Where(m => m.CharacterId == characterGuid).ToList();
    }

    public static IIndustryMembershipRepository Create()
    {
        return new InMemoryIndustryMembershipRepository();
    }

    public void SaveChanges()
    {
        //nothing
    }
}
