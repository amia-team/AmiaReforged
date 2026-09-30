using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Glyph;

[ServiceBinding(typeof(IGlyphIndustryApi))]
public sealed class GlyphIndustryApi : IGlyphIndustryApi
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly IIndustryRepository _industryRepository;
    private readonly IIndustryMembershipRepository _membershipRepository;

    public GlyphIndustryApi(IIndustryRepository industryRepository, IIndustryMembershipRepository membershipRepository)
    {
        _industryRepository = industryRepository;
        _membershipRepository = membershipRepository;
    }

    /// <inheritdoc />
    public List<IndustryMembershipInfo> GetIndustryMemberships(Guid characterId)
    {
        try
        {
            List<IndustryMembership> memberships = _membershipRepository.All(characterId);
            List<IndustryMembershipInfo> result = new(memberships.Count);

            foreach (IndustryMembership m in memberships)
            {
                Industry? industry = _industryRepository.Get(m.IndustryTag);
                string name = industry?.Name ?? m.IndustryTag;
                result.Add(new IndustryMembershipInfo(m.IndustryTag, name, m.Level));
            }

            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[GlyphIndustryApi] GetIndustryMemberships failed for character {CharId}", characterId);
            return [];
        }
    }

    /// <inheritdoc />
    public ProficiencyLevel? GetIndustryLevel(Guid characterId, string industryTag)
    {
        try
        {
            List<IndustryMembership> memberships = _membershipRepository.All(characterId);
            IndustryMembership? match = memberships.FirstOrDefault(
                m => string.Equals(m.IndustryTag, industryTag, StringComparison.OrdinalIgnoreCase));
            return match?.Level;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[GlyphIndustryApi] GetIndustryLevel failed for character {CharId}, industry '{Tag}'",
                characterId, industryTag);
            return null;
        }
    }

    /// <inheritdoc />
    public bool IsIndustryMember(Guid characterId, string industryTag)
    {
        return GetIndustryLevel(characterId, industryTag) != null;
    }

}
