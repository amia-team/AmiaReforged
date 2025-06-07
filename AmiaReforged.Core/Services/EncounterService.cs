using AmiaReforged.Core.Models;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services;

[ServiceBinding(typeof(EncounterService))]
public class EncounterService
{
    private readonly AmiaDbContext _context;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public EncounterService(DatabaseContextFactory context)
    {
        _context = context.CreateDbContext();
    }

    public async Task AddEncounter(Encounter encounter)
    {
        try
        {
            await _context.Encounters.AddAsync(encounter);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to add new encounter: {ex}");
        }
    }

    public IEnumerable<Encounter> GetEncountersForDm(string dmPublicKey)
    {
        try
        {
            IEnumerable<Encounter> encounter = _context.Encounters.Where(e => e.DmId == dmPublicKey);

            foreach (Encounter e in encounter)
            {
                e.EncounterEntries = _context.EncounterEntries.Where(entry => entry.EncounterId == e.Id).ToList();
            }

            return encounter;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to retrieve encounters for {dmPublicKey}: {ex}");
        }

        return new List<Encounter>();
    }
}