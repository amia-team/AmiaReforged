namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits;

/// <summary>
/// In-memory trait store used as a cache backing for <see cref="DbTraitRepository"/>
/// and as a test double.
/// </summary>
public class InMemoryTraitRepository : ITraitRepository
{
    private readonly List<Trait> _traits = [];

    public bool TraitExists(string traitTag)
    {
        return _traits.Any(t => t.Tag == traitTag);
    }

    public List<Trait> All()
    {
        return _traits;
    }

    public void Add(Trait trait)
    {
        Trait? existingTrait = _traits.FirstOrDefault(t => t.Tag == trait.Tag);
        if (existingTrait != null)
        {
            _traits.Remove(existingTrait);
        }

        _traits.Add(trait);
    }

    public bool Remove(string traitTag)
    {
        Trait? existing = _traits.FirstOrDefault(t => t.Tag == traitTag);
        return existing != null && _traits.Remove(existing);
    }

    public Trait? Get(string traitTag)
    {
        return _traits.FirstOrDefault(t => t.Tag == traitTag);
    }

    public static ITraitRepository Create()
    {
        return new InMemoryTraitRepository();
    }
}
