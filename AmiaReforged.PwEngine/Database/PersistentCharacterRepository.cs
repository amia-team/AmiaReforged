using AmiaReforged.PwEngine.Database.Entities;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Database;

using System.Collections.Generic;
using System.Linq;

[ServiceBinding(typeof(IPersistentCharacterRepository))]
public class PersistentCharacterRepository : IPersistentCharacterRepository
{
    private readonly PwContextFactory _factory;

    public PersistentCharacterRepository(PwContextFactory factory)
    {
        _factory = factory;
    }

    public void AddCharacter(PersistedCharacter character)
    {
        using PwEngineContext context = _factory.CreateDbContext();
        context.Characters.Add(character);
        context.SaveChanges();
    }

    public List<PersistedCharacter> GetCharacters()
    {
        using PwEngineContext context = _factory.CreateDbContext();
        return context.Characters.ToList();
    }

    public PersistedCharacter? GetByGuid(Guid id)
    {
        using PwEngineContext context = _factory.CreateDbContext();
        return context.Characters.FirstOrDefault(c => c.Id == id);
    }

    public List<PersistedCharacter> GetCharactersByCdKey(string cdKey)
    {
        using PwEngineContext context = _factory.CreateDbContext();
        return context.Characters
            .Where(c => c.CdKey == cdKey)
            .ToList();
    }

    public void ChangeCharacterOwner(PersistedCharacter character, string cdKey)
    {
        using PwEngineContext context = _factory.CreateDbContext();
        character.CdKey = cdKey;
        context.Characters.Update(character);
        context.SaveChanges();
    }

    public void DeleteCharacter(PersistedCharacter character)
    {
        using PwEngineContext context = _factory.CreateDbContext();
        context.Characters.Remove(character);
        context.SaveChanges();
    }

    public void SaveChanges()
    {
        using PwEngineContext context = _factory.CreateDbContext();
        context.SaveChanges();
    }
}

public interface IPersistentCharacterRepository
{
    void AddCharacter(PersistedCharacter character);
    List<PersistedCharacter> GetCharacters();
    List<PersistedCharacter> GetCharactersByCdKey(string cdKey);
    PersistedCharacter? GetByGuid(Guid id);
    void ChangeCharacterOwner(PersistedCharacter character, string cdKey);
    void DeleteCharacter(PersistedCharacter character);
    void SaveChanges();
}
