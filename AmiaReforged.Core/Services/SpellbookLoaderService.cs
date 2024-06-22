using AmiaReforged.Core.Models;
using AmiaReforged.Core.UserInterface;
using Anvil.API;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;
using NWN.Core;

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

    public void DeleteSpellbook(long selectedSpellbookId, Guid pcId)
    {
        using AmiaDbContext db = _factory.CreateDbContext();

        SavedSpellbook? spellbook = db.SavedSpellbooks.FirstOrDefault(sb => sb.BookId == selectedSpellbookId);

        if (spellbook == null)
        {
            Log.Error($"Spellbook with ID {selectedSpellbookId} not found.");
            return;
        }

        if (spellbook.PlayerCharacterId != pcId)
        {
            Log.Error($"Spellbook with ID {selectedSpellbookId} does not belong to player with ID {pcId}.");
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
            Log.Info($"Spellbook with ID {spellbookId} found in the database.");
            return SpellbookViewModel.FromDatabaseModel(spellbook);
        }

        Log.Error($"Spellbook with ID {spellbookId} not found.");
        return new SpellbookViewModel();
    }
}