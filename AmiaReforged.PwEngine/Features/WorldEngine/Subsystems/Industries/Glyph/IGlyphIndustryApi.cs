using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Glyph;

public interface IGlyphIndustryApi
{
    List<IndustryMembershipInfo> GetIndustryMemberships(Guid characterId);
    ProficiencyLevel? GetIndustryLevel(Guid characterId, string industryTag);
    bool IsIndustryMember(Guid characterId, string industryTag);
}
