using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits;

[ServiceBinding(typeof(ITraitRepository))]
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

    public Trait? Get(string traitTag)
    {
        return _traits.FirstOrDefault(t => t.Tag == traitTag);
    }

    public static ITraitRepository Create()
    {
        return new InMemoryTraitRepository();
    }
}
