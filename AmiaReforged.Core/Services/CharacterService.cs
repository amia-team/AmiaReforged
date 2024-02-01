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
    private readonly AmiaDbContext _ctx;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly NwTaskHelper _nwTaskHelper;

    public CharacterService(AmiaDbContext ctx, NwTaskHelper nwTaskHelper)
    {
        _ctx = ctx;
        _nwTaskHelper = nwTaskHelper;
        Log.Info("NOPE");
    }

    public async Task AddCharacter(PlayerCharacter playerCharacter)
    {
        try
        {
            await _ctx.Characters.AddAsync(playerCharacter);
            await _ctx.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error saving character");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
    }

    public async Task UpdateCharacter(PlayerCharacter playerCharacter)
    {
        try
        {
            _ctx.Characters.Update(playerCharacter);
            await _ctx.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error updating character");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
    }

    public async Task DeleteCharacter(PlayerCharacter playerCharacter)
    {
        try
        {
            _ctx.Characters.Remove(playerCharacter);
            await _ctx.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error deleting character");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
    }

    public async Task<List<PlayerCharacter>> GetAllCharacters()
    {
        List<PlayerCharacter> characters = new();
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

    private async Task<List<PlayerCharacter>> GetCertainCharacters(Expression<Func<PlayerCharacter, bool>> predicate)
    {
        List<PlayerCharacter> characters = new();

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

    public async Task<PlayerCharacter?> GetCharacterByGuid(Guid amiaCharacterId)
    {
        PlayerCharacter? character = null;
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

    public async Task<PlayerCharacter?> GetCharacterFromPcKey(NwItem pcKey)
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

    private Guid PcKeyToGuid(NwItem pcKey) => Guid.Parse(pcKey.Name.Split("_")[1]);

    public async Task AddCharacters(IEnumerable<PlayerCharacter> characters)
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

    public async Task DeleteCharacters(IEnumerable<PlayerCharacter> characters)
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