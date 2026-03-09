namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

public interface IIndustryMembershipRepository
{
    List<IndustryMembership> All(Guid characterGuid);
    void Add(IndustryMembership membership);
    void Update(IndustryMembership membership);
    void Delete(Guid membershipId);

    void SaveChanges();
}
