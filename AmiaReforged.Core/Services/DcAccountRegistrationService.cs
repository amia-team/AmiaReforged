using AmiaReforged.Core.Models;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.Core.Services;

/// <summary>
///   Service that ensures DreamcoinRecord accounts exist for players when they enter the module.
///   Depends on PlayerAccountRegistrationService to ensure Player records exist first.
/// </summary>
[ServiceBinding(typeof(DcAccountRegistrationService))]
public class DcAccountRegistrationService
{
    private readonly DatabaseContextFactory _factory;
    private readonly PlayerAccountRegistrationService _playerAccountService;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public DcAccountRegistrationService(DatabaseContextFactory factory, PlayerAccountRegistrationService playerAccountService)
    {
        _factory = factory;
        _playerAccountService = playerAccountService;
        NwModule.Instance.OnClientEnter += HandleClientEnter;
        Log.Info("DcAccountRegistrationService initialized.");
    }

    private async void HandleClientEnter(ModuleEvents.OnClientEnter eventData)
    {
        try
        {
            if (eventData.Player.IsDM) return;

            string cdKey = eventData.Player.CDKey;

            await EnsureDreamcoinAccountExists(cdKey);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in DcAccountRegistrationService.HandleClientEnter");
        }
    }

    /// <summary>
    ///   Ensures a DreamcoinRecord exists for the given CD key.
    ///   Creates one with 0 balance if it doesn't exist.
    /// </summary>
    private async Task EnsureDreamcoinAccountExists(string cdKey)
    {
        // First ensure the player record exists
        await _playerAccountService.EnsurePlayerExists(cdKey);

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
