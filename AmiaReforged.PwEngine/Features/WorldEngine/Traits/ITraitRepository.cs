namespace AmiaReforged.PwEngine.Features.WorldEngine.Traits;

/// <summary>
/// In-memory registry of trait definitions loaded from JSON
/// </summary>
public interface ITraitRepository
{
    bool TraitExists(string traitTag);
    void Add(Trait trait);
    Trait? Get(string traitTag);
    List<Trait> All();
}
