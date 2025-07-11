using AmiaReforged.Core.Models;
using AmiaReforged.Core.UserInterface;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services;

[ServiceBinding(typeof(SpellbookLoaderService))]
public class SpellbookLoaderService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly DatabaseContextFactory _factory;
    private readonly CharacterService _characterService;

    public SpellbookLoaderService(DatabaseContextFactory factory, CharacterService service)
    {
        Log.Info("SpellbookLoaderService initialized.");
        _factory = factory;
        _characterService = service;
    }

    public List<SpellbookViewModel?> LoadSpellbook(Guid characterId)
    {
        using AmiaDbContext db = _factory.CreateDbContext();

        Log.Info($"Player ID: {characterId}");

        List<SavedSpellbook> spellbookDb = db.SavedSpellbooks.Where(sb => sb.PlayerCharacterId == characterId).ToList();

        Log.Info($"Found number of spellbooks: {spellbookDb.Count}");

        return spellbookDb.Select(SpellbookViewModel.FromDatabaseModel).ToList();
    }

    public async Task SaveSpellbook(SavedSpellbook savedSpellbook)
    {
        await using AmiaDbContext db = _factory.CreateDbContext();
        await db.SavedSpellbooks.AddAsync(savedSpellbook);
        await db.SaveChangesAsync();
    }

    public void DeleteSpellbook(long selectedSpellbookId, Guid characterId)
    {
        using AmiaDbContext db = _factory.CreateDbContext();

        SavedSpellbook? spellbook = db.SavedSpellbooks.FirstOrDefault(sb => sb.BookId == selectedSpellbookId);

        if (spellbook == null)
        {
            Log.Error($"Spellbook with ID {selectedSpellbookId} not found.");
            return;
        }

        if (spellbook.PlayerCharacterId != characterId)
        {
            Log.Error($"Spellbook with ID {selectedSpellbookId} does not belong to player with ID {characterId}.");
            return;
        }

        db.SavedSpellbooks.Remove(spellbook);
        db.SaveChanges();
    }

    public SpellbookViewModel LoadSingleSpellbook(long spellbookId)
    {
        using AmiaDbContext db = _factory.CreateDbContext();

        SavedSpellbook? spellbook = db.SavedSpellbooks.FirstOrDefault(sb => sb.BookId == spellbookId);

        if (spellbook != null)
        {
            return SpellbookViewModel.FromDatabaseModel(spellbook);
        }

        return new SpellbookViewModel();
    }
}