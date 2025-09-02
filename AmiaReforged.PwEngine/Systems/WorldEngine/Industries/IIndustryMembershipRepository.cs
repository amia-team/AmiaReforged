namespace AmiaReforged.PwEngine.Systems.WorldEngine.Industries;

public interface IIndustryMembershipRepository
{
    List<IndustryMembership> All(Guid characterGuid);
    void Add(IndustryMembership membership);
    void Update(IndustryMembership membership);
}
