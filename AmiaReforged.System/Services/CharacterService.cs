using AmiaReforged.Core;
using AmiaReforged.Core.Entities;
using Anvil.API;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.System.Services;

[ServiceBinding(typeof(CharacterService))]
public class CharacterService
{
    private readonly AmiaContext _ctx;
    private readonly Logger Log = LogManager.GetCurrentClassLogger();

    public CharacterService()
    {
        _ctx = new AmiaContext();
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

        await TrySwitchToMainThread();
    }

    public async Task<AmiaCharacter?> GetCharacterByGuid(Guid guid)
    {

        AmiaCharacter? character = await _ctx.Characters.FindAsync(guid);
        await TrySwitchToMainThread();
        
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

        await TrySwitchToMainThread();
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

        await TrySwitchToMainThread();
    }

    private async Task TrySwitchToMainThread()
    {
        try
        {
            await NwTask.SwitchToMainThread();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error switching to main thread");
        }
    }
}