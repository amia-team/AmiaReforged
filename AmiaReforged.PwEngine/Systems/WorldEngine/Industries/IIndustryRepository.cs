namespace AmiaReforged.PwEngine.Systems.WorldEngine.Industries;

public interface IIndustryRepository
{
    bool IndustryExists(string industryTag);
    void Add(Industry industry);
    Industry? Get(string membershipIndustryTag);
    List<Industry> All();
}
