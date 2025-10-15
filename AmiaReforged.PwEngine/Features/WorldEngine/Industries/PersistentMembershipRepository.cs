using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Industries;

[ServiceBinding(typeof(IIndustryMembershipRepository))]
public class PersistentMembershipRepository(IndustryMembershipMapper mapper, PwContextFactory factory)
    : IIndustryMembershipRepository
{
    private readonly PwEngineContext _ctx = factory.CreateDbContext();

    public List<IndustryMembership> All(Guid characterGuid)
    {
        List<PersistentIndustryMembership> memberships =
            _ctx.IndustryMemberships
                .Where(x => x.CharacterId == characterGuid)
                .ToList();

        List<IndustryMembership> domainMemberships = [];
        domainMemberships.AddRange(memberships.Select(mapper.ToDomain)
            .OfType<IndustryMembership>());

        return domainMemberships;
    }

    public void Add(IndustryMembership membership)
    {
        PersistentIndustryMembership membershipEntity = mapper.ToPersistent(membership);

        _ctx.IndustryMemberships.Add(membershipEntity);
    }

    public void Update(IndustryMembership membership)
    {
        PersistentIndustryMembership membershipEntity = mapper.ToPersistent(membership);

        _ctx.IndustryMemberships.Update(membershipEntity);
    }

    public void SaveChanges()
    {
        _ctx.SaveChanges();
    }
}
