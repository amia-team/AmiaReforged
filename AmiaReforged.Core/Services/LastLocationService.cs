using System.Linq.Expressions;
using AmiaReforged.Core.Helpers;
using AmiaReforged.Core.Models;
using Anvil.API;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.Core.Services;

[ServiceBinding(typeof(LastLocationService))]
public class LastLocationService
{
    private readonly DatabaseContextFactory _ctxFactory;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly NwTaskHelper _nwTaskHelper;

    public LastLocationService(DatabaseContextFactory ctxFactory, NwTaskHelper nwTaskHelper)
    {
        _ctxFactory = ctxFactory;
        _nwTaskHelper = nwTaskHelper;
    }

    public async Task AddInvasionArea(LastLocation lastLocation)
    {
        AmiaDbContext amiaDbContext = _ctxFactory.CreateDbContext();
        try
        {
            await amiaDbContext.LastLocation.AddAsync(lastLocation);
            await amiaDbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error saving invasion record");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
    }

    public async Task UpdateInvasionArea(LastLocation lastLocation)
    {
        AmiaDbContext amiaDbContext = _ctxFactory.CreateDbContext();

        try
        {
            amiaDbContext.LastLocation.Update(lastLocation);
            await amiaDbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error updating invasion record");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
    }

    public async Task DeleteInvasionRecord(LastLocation lastLocation)
    {
        AmiaDbContext amiaDbContext = _ctxFactory.CreateDbContext();

        try
        {
            amiaDbContext.LastLocation.Remove(lastLocation);
            await amiaDbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error deleting invasion record");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
    }

    public async Task<List<LastLocation>> GetAllLastLocationRecords()
    {
        AmiaDbContext amiaDbContext = _ctxFactory.CreateDbContext();

        List<LastLocation> lastrecords = new();
        try
        {
            lastrecords = await amiaDbContext.LastLocation.ToListAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error getting all invasion records");
        }
        await _nwTaskHelper.TrySwitchToMainThread();
        return lastrecords; 
    }

    private async Task<List<LastLocation>> GetCertainLastLocationRecord(Expression<Func<LastLocation, bool>> predicate)
    {
        AmiaDbContext amiaDbContext = _ctxFactory.CreateDbContext();

        List<LastLocation> lastrecords = new();
        try
        {
            lastrecords = await amiaDbContext.LastLocation
                .Where(predicate)
                .ToListAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error getting certain invasion record");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
        return lastrecords;
    }
    public async Task<bool> LastLocationRecordExists(string lastlocationid)
    {
        AmiaDbContext amiaDbContext = _ctxFactory.CreateDbContext();

        bool exists = false;
        try
        {
            exists = await amiaDbContext.LastLocation.AnyAsync(c => c.PCKey == lastlocationid);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error checking if last location record exists");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
        
        return exists;
    }

}