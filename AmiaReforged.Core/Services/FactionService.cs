using System.Collections;
using System.Linq.Expressions;
using AmiaReforged.Core.Helpers;
using AmiaReforged.Core.Models;
using AmiaReforged.Core.Models.Faction;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.Core.Services;

// [ServiceBinding(typeof(FactionService))]
public class FactionService
{
    public CharacterService CharacterService { get; }
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly AmiaDbContext _ctx;
    private readonly NwTaskHelper _nwTaskHelper;

    public FactionService(CharacterService characterService, AmiaDbContext ctx, NwTaskHelper nwTaskHelper)
    {
        CharacterService = characterService;
        _ctx = ctx;
        _nwTaskHelper = nwTaskHelper;
    }

    public async Task AddFaction(FactionEntity factionEntity)
    {
        try
        {
            await RemoveNonExistentMembers(factionEntity);

            await _ctx.Factions.AddAsync(factionEntity);
            await _ctx.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error adding faction");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
    }

    private async Task RemoveNonExistentMembers(FactionEntity f)
    {
        if (f is { Members: null }) return;

        foreach (Guid member in f.Members)
        {
            if (await CharacterService.CharacterExists(member)) continue;
            Log.Warn($"Removing non-existent character from faction: {member}");
            f.Members.Remove(member);
        }
    }

    public async Task<FactionEntity?> GetFactionByName(string factionName)
    {
        FactionEntity? faction = await _ctx.Factions.FindAsync(factionName);

        await _nwTaskHelper.TrySwitchToMainThread();

        return faction;
    }

    public async Task DeleteFaction(FactionEntity factionEntity)
    {
        try
        {
            _ctx.Factions.Remove(factionEntity);
            await _ctx.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error deleting faction");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
    }

    public async Task UpdateFaction(FactionEntity f)
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

    public async Task AddToRoster(FactionEntity factionEntity, Guid id)
    {
        try
        {
            ((IList)factionEntity.Members).Add(id);
            _ctx.Factions.Update(factionEntity);
            await _ctx.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error adding character to roster");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
    }

    public async Task AddToRoster(FactionEntity factionEntity, IEnumerable<Guid> characters)
    {
        try
        {
            foreach (Guid id in characters)
            {
                factionEntity.Members.Add(id);
            }

            await _ctx.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error adding character to roster");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
    }

    public async Task<IEnumerable<FactionEntity>> GetAllFactions()
    {
        IEnumerable<FactionEntity> factions = await _ctx.Factions.ToListAsync();

        await _nwTaskHelper.TrySwitchToMainThread();

        return factions;
    }

    public async Task RemoveFromRoster(FactionEntity factionEntity, Guid characterId)
    {
        try
        {
            factionEntity.Members.Remove(characterId);
            _ctx.Factions.Update(factionEntity);
            await _ctx.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error removing character from roster");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
    }

    private async Task<List<PlayerCharacter>> GetCharactersByCriteria(FactionEntity f, Expression<Func<PlayerCharacter, bool>> criteria)
    {
        List<PlayerCharacter> characters = new();

        foreach (Guid id in f.Members)
        {
            IQueryable<PlayerCharacter> query = _ctx.Characters.Where(c => c.Id == id);

            if (criteria != null)
            {
                query = query.Where(criteria);
            }

            PlayerCharacter? character = await query.SingleOrDefaultAsync();

            if (character is not null)
            {
                characters.Add(character);
            }
        }

        return characters;
    }

    public async Task DeleteFactions(List<FactionEntity> faction)
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

    public async Task AddFactions(List<FactionEntity> factions)
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