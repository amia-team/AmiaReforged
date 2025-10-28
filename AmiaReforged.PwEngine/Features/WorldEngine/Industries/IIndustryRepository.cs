using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Industries;

public interface IIndustryRepository
{
    bool IndustryExists(string industryTag);
    void Add(Industry industry);
    Industry? Get(string membershipIndustryTag);
    Industry? GetByTag(IndustryTag industryTag);
    List<Industry> All();
}
