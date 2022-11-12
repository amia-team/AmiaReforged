using System.Collections;
using AmiaReforged.Core;
using AmiaReforged.Core.Entities;
using AmiaReforged.Core.Models;
using AmiaReforged.System.Helpers;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.System.Services;

public class FactionService
{
    private readonly CharacterService _characterService;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly AmiaContext _ctx;
    private readonly NwTaskHelper _nwTaskHelper;

    public FactionService(CharacterService characterService)
    {
        _characterService = characterService;
        _ctx = new AmiaContext();
        _nwTaskHelper = new NwTaskHelper();
    }

    public async Task AddFaction(Faction faction)
    {
        try
        {
            List<AmiaCharacter> characters = await _ctx.Characters.ToListAsync();

            RemoveNonExistentMembers(faction);

            await _ctx.Factions.AddAsync(faction);
            await _ctx.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error adding faction");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
    }

    private async void RemoveNonExistentMembers(Faction f)
    {
        foreach (Guid member in f.Members)
        {
            if (await _characterService.CharacterExists(member)) continue;
            Log.Warn($"Removing non-existent character from faction: {member}");
            f.Members.Remove(member);
        }
    }

    public async Task<Faction?> GetFactionByName(string factionName)
    {
        Faction? faction = await _ctx.Factions.FindAsync(factionName);

        await _nwTaskHelper.TrySwitchToMainThread();

        return faction;
    }

    public async Task DeleteFaction(Faction faction)
    {
        try
        {
            _ctx.Factions.Remove(faction);
            await _ctx.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error deleting faction");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
    }

    public async Task UpdateFaction(Faction f)
    {
        try
        {
            _ctx.Factions.Update(f);

            await _ctx.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error updating faction");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
    }

    public async Task AddToRoster(Faction faction, Guid id)
    {
        try
        {
            ((IList)faction.Members).Add(id);
            _ctx.Factions.Update(faction);
            await _ctx.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error adding character to roster");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
    }

    public async Task AddToRoster(Faction faction, IEnumerable<Guid> characters)
    {
        try
        {
            foreach (Guid id in characters)
            {
                faction.Members.Add(id);
            }

            await _ctx.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error adding character to roster");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
    }

    public async Task<IEnumerable<Faction>> GetAllFactions()
    {
        IEnumerable<Faction> factions = await _ctx.Factions.ToListAsync();

        await _nwTaskHelper.TrySwitchToMainThread();

        return factions;
    }

    public async Task RemoveFromRoster(Faction faction, Guid characterId)
    {
        try
        {
            faction.Members.Remove(characterId);
            _ctx.Factions.Update(faction);
            await _ctx.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error removing character from roster");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
    }

    public async Task<List<AmiaCharacter>> GetAllCharacters(Faction f)
    {
        List<AmiaCharacter> characters = new();
        try
        {
            foreach (Guid id in f.Members)
            {
                AmiaCharacter? character = await _characterService.GetCharacterById(id);
                if(character is null) continue;
                characters.Add(character);
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Error getting all characters in roster");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
        return characters;
    }
    public async Task<List<AmiaCharacter>> GetAllPlayerCharactersFrom(Faction faction)
    {
        List<AmiaCharacter> characters = await GetAllCharacters(faction);
        return characters.Where(c => c.IsPlayerCharacter).ToList();
    }

    public async Task<List<AmiaCharacter>> GetAllNonPlayerCharactersFrom(Faction faction)
    {
        List<AmiaCharacter> characters = await GetAllCharacters(faction);
        return characters.Where(c => !c.IsPlayerCharacter).ToList();
    }
}