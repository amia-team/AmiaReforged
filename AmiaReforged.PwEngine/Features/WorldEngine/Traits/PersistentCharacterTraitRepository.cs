using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Traits;

[ServiceBinding(typeof(ICharacterTraitRepository))]
public class PersistentCharacterTraitRepository(CharacterTraitMapper mapper, PwContextFactory factory)
    : ICharacterTraitRepository
{
    public List<CharacterTrait> GetByCharacterId(Guid characterId)
    {
        using PwEngineContext ctx = factory.CreateDbContext();
        
        List<PersistentCharacterTrait> persistent = ctx.CharacterTraits
            .Where(t => t.CharacterId == characterId)
            .ToList();

        return persistent.Select(mapper.ToDomain).ToList();
    }

    public void Add(CharacterTrait trait)
    {
        using PwEngineContext ctx = factory.CreateDbContext();
        
        PersistentCharacterTrait persistent = mapper.ToPersistent(trait);
        ctx.CharacterTraits.Add(persistent);
        ctx.SaveChanges();
    }

    public void Update(CharacterTrait trait)
    {
        using PwEngineContext ctx = factory.CreateDbContext();
        
        PersistentCharacterTrait? existing = ctx.CharacterTraits
            .FirstOrDefault(t => t.Id == trait.Id);

        if (existing == null) return;

        existing.IsConfirmed = trait.IsConfirmed;
        existing.IsActive = trait.IsActive;
        existing.CustomData = trait.CustomData;

        ctx.SaveChanges();
    }

    public void Delete(Guid traitId)
    {
        using PwEngineContext ctx = factory.CreateDbContext();
        
        PersistentCharacterTrait? existing = ctx.CharacterTraits
            .FirstOrDefault(t => t.Id == traitId);

        if (existing == null) return;

        ctx.CharacterTraits.Remove(existing);
        ctx.SaveChanges();
    }
}
