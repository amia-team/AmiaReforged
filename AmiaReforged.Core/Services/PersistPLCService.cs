using System.Linq.Expressions;
using AmiaReforged.Core.Models;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.Core.Services;

// [ServiceBinding(typeof(PersistPLCService))]
public class PersistPLCService
{
    private readonly DatabaseContextFactory _ctxFactory;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public PersistPLCService(DatabaseContextFactory ctxFactory)
    {
        _ctxFactory = ctxFactory;
    }

    public async Task AddPersistPLC(PersistPLC persistPLC)
    {
        AmiaDbContext amiaDbContext = _ctxFactory.CreateDbContext();
        try
        {
            await amiaDbContext.PersistPLC.AddAsync(persistPLC);
            await amiaDbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error saving invasion record");
        }

    }

    public async Task UpdatePersistPLC(PersistPLC persistPLC)
    {
        AmiaDbContext amiaDbContext = _ctxFactory.CreateDbContext();

        try
        {
            amiaDbContext.PersistPLC.Update(persistPLC);
            await amiaDbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error updating invasion record");
        }

    }

    public async Task DeletePersistPLC(PersistPLC persistPLC)
    {
        AmiaDbContext amiaDbContext = _ctxFactory.CreateDbContext();

        try
        {
            amiaDbContext.PersistPLC.Remove(persistPLC);
            await amiaDbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error deleting persistent plc record");
        }

    }

    public async Task<List<PersistPLC>> GetAllPersistPLCRecords()
    {
        AmiaDbContext amiaDbContext = _ctxFactory.CreateDbContext();

        List<PersistPLC> persistplc = new();
        try
        {
            persistplc = await amiaDbContext.PersistPLC.ToListAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error getting all invasion records");
        }

        return persistplc; 
    }

    private async Task<List<PersistPLC>> GetCertainPersistPLCRecord(Expression<Func<PersistPLC, bool>> predicate)
    {
        AmiaDbContext amiaDbContext = _ctxFactory.CreateDbContext();

        List<PersistPLC> persistplc = new();
        try
        {
            persistplc = await amiaDbContext.PersistPLC
                .Where(predicate)
                .ToListAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error getting certain invasion record");
        }

        return persistplc;
    }

    /* Not relevant for this data but keeping this to rework in future if needed
    public async Task<bool> PersistPLCRecordExists(string persistplcid)
    {
        AmiaDbContext amiaDbContext = _ctxFactory.CreateDbContext();

        bool exists = false;
        try
        {
            exists = await amiaDbContext.PersistPLC.AnyAsync(c => c.PLC == persistplcid);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error checking if last location record exists");
        }

        await _nwTaskHelper.TrySwitchToMainThread();
        
        return exists;
    }
    */

}