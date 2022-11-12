using AmiaReforged.Core;
using AmiaReforged.Core.Entities;
using AmiaReforged.System.Helpers;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.System.Services;

// [ServiceBinding(typeof(CharacterService))]
public class CharacterService
{
    private readonly AmiaContext _ctx;
    private readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly NwTaskHelper _nwTaskHelper;

    public CharacterService()
    {
        _ctx = new AmiaContext();
        _nwTaskHelper = new NwTaskHelper();
    }

    public async Task AddCharacter(AmiaCharacter character)
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

    public async Task<AmiaCharacter?> GetCharacterByGuid(Guid guid)
    {
        AmiaCharacter? character = await _ctx.Characters.FindAsync(guid);
        await _nwTaskHelper.TrySwitchToMainThread();

        return character;
    }

    public async Task UpdateCharacter(AmiaCharacter character)
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

    public async Task DeleteCharacter(AmiaCharacter character)
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

    public async Task<List<AmiaCharacter>> GetAllCharacters()
    {
        List<AmiaCharacter> characters = new List<AmiaCharacter>();
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

    public async Task<List<AmiaCharacter>> GetAllPlayerCharacters()
    {
        List<AmiaCharacter> characters = new();
        try
        {
            characters = await _ctx.Characters.Where(c => c.IsPlayerCharacter).ToListAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error getting all player characters");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
        return characters;
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

    public async Task<AmiaCharacter?> GetCharacterById(Guid amiaCharacterId)
    { 
        AmiaCharacter? character = null;
        try
        {
            character = await _ctx.Characters.FindAsync(amiaCharacterId);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error getting character by id");
        }

        return character;
    }
}