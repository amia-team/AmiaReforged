using AmiaReforged.Core;
using AmiaReforged.Core.Entities;
using Anvil.API;
using Anvil.Services;
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
    
    public async void AddCharacter(AmiaCharacter character)
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

        await NwTask.SwitchToMainThread();
    }
    public AmiaCharacter? GetCharacter(Guid pcKey) => _ctx.Characters.FirstOrDefault(c => c!.PcId == pcKey);

    public void UpdateCharacter(AmiaCharacter? character)
    {
        try
        {
            _ctx.Characters.Update(character);
            _ctx.SaveChanges();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error updating character");
        }
    }

    public void DeleteCharacter(AmiaCharacter? character)
    {
        try
        {
            _ctx.Characters.Remove(character);
            _ctx.SaveChanges();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error deleting character");
        }
    }
}