using AmiaReforged.Core.Models;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.Core.Services;

[ServiceBinding(typeof(EncounterService))]
public class EncounterService
{
    private readonly DatabaseContextFactory _factory;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public EncounterService(DatabaseContextFactory context)
    {
        _factory = context;
    }

    public void AddEncounter(Encounter encounter)
    {
        AmiaDbContext ctx = _factory.CreateDbContext();
        try
        {
            ctx.Encounters.Add(encounter);
            ctx.SaveChanges();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to add new encounter: {ex}");
        }
    }

    public void DeleteEncounter(Encounter encounter)
    {
        AmiaDbContext ctx = _factory.CreateDbContext();

        try
        {
            ctx.Encounters.Remove(encounter);
            ctx.SaveChanges();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to delete encounter: {ex}");
        }
    }

    public IEnumerable<Encounter> GetEncountersForDm(string dmPublicKey)
    {
        AmiaDbContext ctx = _factory.CreateDbContext();

        try
        {
            IEnumerable<Encounter> encounter =
                ctx
                    .Encounters
                    .Include(p => p.EncounterEntries)
                    .Where(e => e.DmId == dmPublicKey);

            Log.Info($"Found {encounter.Count()} encounters");

            return encounter;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to retrieve encounters for {dmPublicKey}: {ex}");
        }

        return new List<Encounter>();
    }

    public void AddEncounterEntry(EncounterEntry entry)
    {
        AmiaDbContext ctx = _factory.CreateDbContext();

        try
        {
            ctx.EncounterEntries.Add(entry);
            ctx.SaveChanges();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to add an entry to this encounter: {ex}");
        }
    }

    public void DeleteEntry(EncounterEntry entry)
    {
        AmiaDbContext ctx = _factory.CreateDbContext();

        try
        {
            ctx.EncounterEntries.Remove(entry);
            ctx.SaveChanges();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to add an entry to this encounter: {ex}");
        }
    }

    public IEnumerable<EncounterEntry> GetEntries(long encounterId)
    {
        List<EncounterEntry> entries = [];
        AmiaDbContext ctx = _factory.CreateDbContext();

        try
        {
            entries = ctx.EncounterEntries.Where(e => e.EncounterId == encounterId).ToList();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to retrieve entries for {encounterId}: {ex}");
        }

        
        return entries;
    }
}