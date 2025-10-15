using AmiaReforged.PwEngine.Features.WorldEngine.Industries;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Characters;

public interface ICharacterIndustryContext
{
    void JoinIndustry(string industryTag);
    List<IndustryMembership> AllIndustryMemberships();
    RankUpResult RankUp(string industryTag);
}
