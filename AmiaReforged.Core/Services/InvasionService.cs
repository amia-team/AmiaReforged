using System.Linq.Expressions;
using AmiaReforged.Core.Models;
using Anvil.API;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.Core.Services;

// [ServiceBinding(typeof(InvasionService))]
public class InvasionService
{
    private readonly DatabaseContextFactory _ctxFactory;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public InvasionService(DatabaseContextFactory ctxFactory)
    {
        _ctxFactory = ctxFactory;
    }

    public async Task AddInvasionArea(InvasionRecord invasionRecord)
    {
        AmiaDbContext amiaDbContext = _ctxFactory.CreateDbContext();
        try
        {
            await amiaDbContext.InvasionRecord.AddAsync(invasionRecord);
            await amiaDbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error saving invasion record");
        }
    }

    public async Task UpdateInvasionArea(InvasionRecord invasionRecord)
    {
        AmiaDbContext amiaDbContext = _ctxFactory.CreateDbContext();

        try
        {
            amiaDbContext.InvasionRecord.Update(invasionRecord);
            await amiaDbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error updating invasion record");
        }

        await NwTask.SwitchToMainThread();
    }

    public async Task DeleteInvasionRecord(InvasionRecord invasionRecord)
    {
        AmiaDbContext amiaDbContext = _ctxFactory.CreateDbContext();

        try
        {
            amiaDbContext.InvasionRecord.Remove(invasionRecord);
            await amiaDbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error deleting invasion record");
        }

        await NwTask.SwitchToMainThread();
    }

    public async Task<List<InvasionRecord>> GetAllInvasionRecords()
    {
        AmiaDbContext amiaDbContext = _ctxFactory.CreateDbContext();

        List<InvasionRecord> invasions = new();
        try
        {
            invasions = await amiaDbContext.InvasionRecord.ToListAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error getting all invasion records");
        }
        await NwTask.SwitchToMainThread();
        return invasions;
    }

    private async Task<List<InvasionRecord>> GetCertainInvasionRecord(Expression<Func<InvasionRecord, bool>> predicate)
    {
        AmiaDbContext amiaDbContext = _ctxFactory.CreateDbContext();

        List<InvasionRecord> invasions = new();
        try
        {
            invasions = await amiaDbContext.InvasionRecord
                .Where(predicate)
                .ToListAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error getting certain invasion record");
        }

        await NwTask.SwitchToMainThread();
        return invasions;
    }
    public async Task<bool> InvasionRecordExists(string invasionId)
    {
        AmiaDbContext amiaDbContext = _ctxFactory.CreateDbContext();

        bool exists = false;
        try
        {
            exists = await amiaDbContext.InvasionRecord.AnyAsync(c => c.AreaZone == invasionId);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error checking if invasion record exists");
        }

        await NwTask.SwitchToMainThread();

        return exists;
    }

}
