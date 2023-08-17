using System.Linq.Expressions;
using AmiaReforged.Core.Helpers;
using AmiaReforged.Core.Models;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.Core.Services;

// [ServiceBinding(typeof(CharacterService))]
public class CharacterService
{
    private readonly AmiaDbContext _ctx;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly NwTaskHelper _nwTaskHelper;

    public CharacterService(AmiaDbContext ctx, NwTaskHelper nwTaskHelper)
    {
        _ctx = ctx;
        _nwTaskHelper = nwTaskHelper;
    }

    public async Task AddCharacter(Character character)
    {
        try
        {
            await _ctx.Characters.AddAsync(character);
            await _ctx.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error saving character");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
    }

    public async Task UpdateCharacter(Character character)
    {
        try
        {
            _ctx.Characters.Update(character);
            await _ctx.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error updating character");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
    }

    public async Task DeleteCharacter(Character character)
    {
        try
        {
            _ctx.Characters.Remove(character);
            await _ctx.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error deleting character");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
    }

    public async Task<List<Character>> GetAllCharacters()
    {
        List<Character> characters = new();
        try
        {
            characters = await _ctx.Characters.ToListAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error getting all characters");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
        return characters;
    }

    private async Task<List<Character>> GetCertainCharacters(Expression<Func<Character, bool>> predicate)
    {
        List<Character> characters = new();

        try
        {
            characters = await _ctx.Characters
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

    public Task<List<Character>> GetAllPlayerCharacters()
    {
        return GetCertainCharacters(character => character.IsPlayerCharacter);
    }

    public Task<List<Character>> GetAllNonPlayerCharacters()
    {
        return GetCertainCharacters(character => !character.IsPlayerCharacter);
    }

    public async Task<bool> CharacterExists(Guid amiaCharacterId)
    {
        bool exists = false;
        try
        {
            exists = await _ctx.Characters.AnyAsync(c => c.Id == amiaCharacterId);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error checking if character exists");
        }

        return exists;
    }

    public async Task<Character?> GetCharacterByGuid(Guid amiaCharacterId)
    {
        Character? character = null;
        try
        {
            character = await _ctx.Characters.AsNoTracking().FirstOrDefaultAsync(c => c.Id == amiaCharacterId);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error getting character by id");
        }

        return character;
    }

    public async Task AddCharacters(IEnumerable<Character> characters)
    {
        try
        {
            await _ctx.Characters.AddRangeAsync(characters);
            await _ctx.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error adding characters");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
    }

    public async Task DeleteCharacters(IEnumerable<Character> characters)
    {
        try
        {
            _ctx.Characters.RemoveRange(characters);
            await _ctx.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error deleting characters");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
    }
}