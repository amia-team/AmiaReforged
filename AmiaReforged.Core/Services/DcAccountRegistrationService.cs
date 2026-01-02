using AmiaReforged.Core.Models;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.Core.Services;

/// <summary>
///   Service that ensures DreamcoinRecord accounts exist for players when they enter the module.
/// </summary>
[ServiceBinding(typeof(DcAccountRegistrationService))]
public class DcAccountRegistrationService
{
    private readonly DatabaseContextFactory _factory;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public DcAccountRegistrationService(DatabaseContextFactory factory)
    {
        _factory = factory;
        NwModule.Instance.OnClientEnter += HandleClientEnter;
        Log.Info("DcAccountRegistrationService initialized.");
    }

    private async void HandleClientEnter(ModuleEvents.OnClientEnter eventData)
    {
        if (eventData.Player.IsDM) return;

        string cdKey = eventData.Player.CDKey;
        
        await EnsureDreamcoinAccountExists(cdKey);
    }

    /// <summary>
    ///   Ensures a DreamcoinRecord exists for the given CD key.
    ///   Creates one with 0 balance if it doesn't exist.
    /// </summary>
    private async Task EnsureDreamcoinAccountExists(string cdKey)
    {
        await using AmiaDbContext context = _factory.CreateDbContext();
        try
        {
            bool exists = await context.DreamcoinRecords
                .AnyAsync(r => r.CdKey == cdKey);

            if (exists)
            {
                await NwTask.SwitchToMainThread();
                return;
            }

            // First ensure the player record exists
            bool playerExists = await context.Players
                .AnyAsync(p => p.CdKey == cdKey);

            if (!playerExists)
            {
                Log.Warn($"Player record does not exist for {cdKey}, cannot create Dreamcoin account yet.");
                await NwTask.SwitchToMainThread();
                return;
            }

            DreamcoinRecord newRecord = new()
            {
                CdKey = cdKey,
                Amount = 0
            };

            context.DreamcoinRecords.Add(newRecord);
            await context.SaveChangesAsync();
            Log.Info($"Created Dreamcoin account for {cdKey}.");
            await NwTask.SwitchToMainThread();
        }
        catch (Exception e)
        {
            Log.Error($"Error ensuring Dreamcoin account exists for {cdKey}: {e.Message}");
            await NwTask.SwitchToMainThread();
        }
    }
}
