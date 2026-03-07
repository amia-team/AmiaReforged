using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

/// <summary>
/// In-memory implementation retained for unit testing and caching scenarios.
/// The DB-backed DbIndustryRepository is now the active service binding.
/// </summary>
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
        Industry? existingIndustry = _industries.FirstOrDefault(i => i.Tag == industry.Tag);
        if (existingIndustry != null)
        {
            _industries.Remove(existingIndustry);
        }

        _industries.Add(industry);
    }

    public Industry? Get(string membershipIndustryTag)
    {
        return _industries.FirstOrDefault(i => i.Tag == membershipIndustryTag);
    }

    public Industry? GetByTag(IndustryTag industryTag)
    {
        return _industries.FirstOrDefault(i => i.Tag == industryTag.Value);
    }

    public void Update(Industry industry)
    {
        Industry? existing = _industries.FirstOrDefault(i => i.Tag == industry.Tag);
        if (existing != null) _industries.Remove(existing);
        _industries.Add(industry);
    }

    public bool Delete(string tag)
    {
        Industry? existing = _industries.FirstOrDefault(i => i.Tag == tag);
        if (existing == null) return false;
        _industries.Remove(existing);
        return true;
    }

    public List<Industry> Search(string? searchTerm, int page, int pageSize, out int totalCount)
    {
        IEnumerable<Industry> query = _industries;
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(i =>
                i.Tag.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                i.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        var filtered = query.OrderBy(i => i.Name).ToList();
        totalCount = filtered.Count;
        return filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
    }

    public static IIndustryRepository Create()
    {
        return new InMemoryIndustryRepository();
    }
}
