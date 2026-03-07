using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

public interface IIndustryRepository
{
    bool IndustryExists(string industryTag);
    void Add(Industry industry);
    Industry? Get(string membershipIndustryTag);
    Industry? GetByTag(IndustryTag industryTag);
    List<Industry> All();
    void Update(Industry industry);
    bool Delete(string tag);
    List<Industry> Search(string? searchTerm, int page, int pageSize, out int totalCount);
}
