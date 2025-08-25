namespace AmiaReforged.PwEngine.Systems.WorldEngine.Industries;

public interface IIndustryMembershipService
{
    void AddMembership(IndustryMembership membership);
    List<IndustryMembership> GetMemberships(Guid characterGuid);
    RankUpResult RankUp(IndustryMembership membership);
}

public enum RankUpResult
{
    Success,
    InsufficientKnowledge,
    AlreadyMaxedOut,
    IndustryNotFound
}
