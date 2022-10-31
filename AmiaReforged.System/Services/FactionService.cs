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
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly AmiaContext _ctx;
    private readonly NwTaskHelper _nwTaskHelper;

    public FactionService()
    {
        _ctx = new AmiaContext();
        _nwTaskHelper = new NwTaskHelper();
    }

    public async Task AddFaction(Faction faction)
    {
        try
        {
            await _ctx.Factions.AddAsync(faction);
            await _ctx.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error adding faction");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
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
}