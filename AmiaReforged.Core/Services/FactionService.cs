using System.Collections;
using AmiaReforged.Core.Helpers;
using AmiaReforged.Core.Models;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.Core.Services;

// [ServiceBinding(typeof(FactionService))]
public class FactionService
{
    public CharacterService CharacterService { get; }
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly AmiaContext _ctx;
    private readonly NwTaskHelper _nwTaskHelper;

    public FactionService(CharacterService characterService, AmiaContext ctx, NwTaskHelper nwTaskHelper)
    {
        CharacterService = characterService;
        _ctx = ctx;
        _nwTaskHelper = nwTaskHelper;
    }

    public async Task AddFaction(Faction faction)
    {
        try
        {
            await RemoveNonExistentMembers(faction);

            await _ctx.Factions.AddAsync(faction);
            await _ctx.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error adding faction");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
    }

    private async Task RemoveNonExistentMembers(Faction f)
    {
        if (f is { Members: null }) return;

        foreach (Guid member in f.Members)
        {
            if (await CharacterService.CharacterExists(member)) continue;
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

    public async Task<List<Character>> GetAllCharacters(Faction f)
    {
        List<Character> characters = new();
        try
        {
            foreach (Guid id in f.Members)
            {
                Character? character = await CharacterService.GetCharacterByGuid(id);
                if (character is null) continue;
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

    public async Task<List<Character>> GetAllPlayerCharactersFrom(Faction faction)
    {
        List<Character> characters = await GetAllCharacters(faction);
        return characters.Where(c => c.IsPlayerCharacter).ToList();
    }

    public async Task<List<Character>> GetAllNonPlayerCharactersFrom(Faction faction)
    {
        List<Character> characters = await GetAllCharacters(faction);
        return characters.Where(c => !c.IsPlayerCharacter).ToList();
    }

    public async Task DeleteFactions(List<Faction> faction)
    {
        try
        {
            _ctx.Factions.RemoveRange(faction);
            await _ctx.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error deleting factions");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
    }

    public async Task AddFactions(List<Faction> factions)
    {
        try
        {
            await _ctx.Factions.AddRangeAsync(factions);
            await _ctx.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error adding factions");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
    }

    public async Task<bool> DoesFactionExist(string relationFactionName)
    {
        bool found = false;
        try
        {
            found = await _ctx.Factions.AnyAsync(f => f.Name == relationFactionName);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error checking if faction exists");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
        return found;
    }
}