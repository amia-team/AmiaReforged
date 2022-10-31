using AmiaReforged.Core;
using AmiaReforged.Core.Entities;
using AmiaReforged.System.Helpers;
using Anvil.Services;
using NLog;

namespace AmiaReforged.System.Services;

[ServiceBinding(typeof(CharacterService))]
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
}