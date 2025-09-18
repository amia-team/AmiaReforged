using AmiaReforged.PwEngine.Systems.WorldEngine.Industries;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Characters;

public interface ICharacterIndustryContext
{
    void JoinIndustry(string industryTag);
    List<IndustryMembership> AllIndustryMemberships();
    RankUpResult RankUp(string industryTag);
}