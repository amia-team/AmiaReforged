using System.Linq.Expressions;
using AmiaReforged.Core.Helpers;
using AmiaReforged.Core.Models;
using Anvil.API;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.Core.Services;

[ServiceBinding(typeof(CharacterService))]
public class CharacterService
{
    private readonly DatabaseContextFactory _ctxFactory;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly NwTaskHelper _nwTaskHelper;

    public CharacterService(DatabaseContextFactory ctxFactory, NwTaskHelper nwTaskHelper)
    {
        _ctxFactory = ctxFactory;
        _nwTaskHelper = nwTaskHelper;
    }

    public async Task AddCharacter(PlayerCharacter playerCharacter)
    {
        AmiaDbContext amiaDbContext = _ctxFactory.CreateDbContext();
        try
        {
            await amiaDbContext.Characters.AddAsync(playerCharacter);
            await amiaDbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error saving character");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
    }

    public async Task UpdateCharacter(PlayerCharacter playerCharacter)
    {
        AmiaDbContext amiaDbContext = _ctxFactory.CreateDbContext();

        try
        {
            amiaDbContext.Characters.Update(playerCharacter);
            await amiaDbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error updating character");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
    }

    public async Task DeleteCharacter(PlayerCharacter playerCharacter)
    {
        AmiaDbContext amiaDbContext = _ctxFactory.CreateDbContext();

        try
        {
            amiaDbContext.Characters.Remove(playerCharacter);
            await amiaDbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error deleting character");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
    }

    public async Task<List<PlayerCharacter>> GetAllCharacters()
    {
        AmiaDbContext amiaDbContext = _ctxFactory.CreateDbContext();

        List<PlayerCharacter> characters = new();
        try
        {
            characters = await amiaDbContext.Characters.ToListAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error getting all characters");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
        return characters;
    }

    private async Task<List<PlayerCharacter>> GetCertainCharacters(Expression<Func<PlayerCharacter, bool>> predicate)
    {
        AmiaDbContext amiaDbContext = _ctxFactory.CreateDbContext();

        List<PlayerCharacter> characters = new();
        try
        {
            characters = await amiaDbContext.Characters
                .Where(predicate)
                .ToListAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error getting certain characters");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
        return characters;
    }

    public async Task<bool> CharacterExists(Guid amiaCharacterId)
    {
        AmiaDbContext amiaDbContext = _ctxFactory.CreateDbContext();

        bool exists = false;
        try
        {
            exists = await amiaDbContext.Characters.AnyAsync(c => c.Id == amiaCharacterId);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error checking if character exists");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
        
        return exists;
    }

    public async Task<PlayerCharacter?> GetCharacterByGuid(Guid amiaCharacterId)
    {
        AmiaDbContext amiaDbContext = _ctxFactory.CreateDbContext();

        PlayerCharacter? character = null;
        try
        {
            character = await amiaDbContext.Characters
            .Include(c => c.Items) // Eager load StoredItems
            .FirstOrDefaultAsync(c => c.Id == amiaCharacterId);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error getting character by id");
        }

        return character;
    }

    public async Task<PlayerCharacter?> GetCharacterFromPcKey(NwItem? pcKey)
    {
        PlayerCharacter? character = null;
        if (pcKey.Tag != "ds_pckey")
        {
            return character;
        }
        
        Guid charId = PcKeyToGuid(pcKey);

        character = await GetCharacterByGuid(charId);

        return character;
    }

    private Guid PcKeyToGuid(NwItem? pcKey) => Guid.Parse(pcKey.Name.Split("_")[1]);

    public async Task AddCharacters(IEnumerable<PlayerCharacter> characters)
    {
        AmiaDbContext amiaDbContext = _ctxFactory.CreateDbContext();

        try
        {
            await amiaDbContext.Characters.AddRangeAsync(characters);
            await amiaDbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error adding characters");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
    }

    public async Task DeleteCharacters(IEnumerable<PlayerCharacter> characters)
    {
        AmiaDbContext amiaDbContext = _ctxFactory.CreateDbContext();

        try
        {
            amiaDbContext.Characters.RemoveRange(characters);
            await amiaDbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error deleting characters");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
    }
}