using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;

public interface ICharacterIndustryContext
{
    void JoinIndustry(string industryTag);
    List<IndustryMembership> AllIndustryMemberships();
    RankUpResult RankUp(string industryTag);
}
