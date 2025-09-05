namespace AmiaReforged.PwEngine.Systems.WorldEngine.Industries;

public class InMemoryIndustryRepository : IIndustryRepository
{
    private readonly List<Industry> _industries = [];

    public bool IndustryExists(string industryTag)
    {
        return _industries.Any(i => i.Tag == industryTag);
    }

    public List<Industry> All()
    {
        return _industries;
        }

    public void Add(Industry industry)
    {
        _industries.Add(industry);
    }

    public Industry? Get(string membershipIndustryTag)
    {
        return _industries.FirstOrDefault(i => i.Tag == membershipIndustryTag);
    }

    public static IIndustryRepository Create()
    {
        return new InMemoryIndustryRepository();
    }
}
