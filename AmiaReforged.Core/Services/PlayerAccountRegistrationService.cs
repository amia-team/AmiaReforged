using AmiaReforged.Core.Models;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.Core.Services;

/// <summary>
///   Service that ensures Player records exist for new CD keys when they enter the module.
///   This service must run before other services that depend on Player records existing,
///   such as DcAccountRegistrationService and PlayerPlaytimeService.
/// </summary>
[ServiceBinding(typeof(PlayerAccountRegistrationService))]
public class PlayerAccountRegistrationService
{
    private readonly DatabaseContextFactory _factory;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public PlayerAccountRegistrationService(DatabaseContextFactory factory)
    {
        _factory = factory;
        NwModule.Instance.OnClientEnter += HandleClientEnter;
        Log.Info("PlayerAccountRegistrationService initialized.");
    }

    private async void HandleClientEnter(ModuleEvents.OnClientEnter eventData)
    {
        if (eventData.Player.IsDM) return;

        string cdKey = eventData.Player.CDKey;

        await EnsurePlayerExists(cdKey);
    }

    /// <summary>
    ///   Ensures a Player record exists for the given CD key.
    ///   Creates one if it doesn't exist.
    /// </summary>
    /// <param name="cdKey">The player's CD key.</param>
    public async Task EnsurePlayerExists(string cdKey)
    {
        await using AmiaDbContext context = _factory.CreateDbContext();
        try
        {
            bool exists = await context.Players
                .AnyAsync(p => p.CdKey == cdKey);

            if (exists)
            {
                await NwTask.SwitchToMainThread();
                return;
            }

            Player newPlayer = new()
            {
                CdKey = cdKey
            };

            context.Players.Add(newPlayer);
            await context.SaveChangesAsync();
            Log.Info($"Created Player account for new CD key: {cdKey}");
            await NwTask.SwitchToMainThread();
        }
        catch (Exception e)
        {
            Log.Error($"Error ensuring Player account exists for {cdKey}: {e.Message}");
            await NwTask.SwitchToMainThread();
        }
    }
}
