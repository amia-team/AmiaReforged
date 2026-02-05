using AmiaReforged.Core.Models;
using Anvil.API;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.Core.Services;

/// <summary>
///   Service for managing Dreamcoin balances in the database.
///   Account creation is handled by DcAccountRegistrationService on player login.
/// </summary>
[ServiceBinding(typeof(DreamcoinService))]
public class DreamcoinService
{
    private readonly DatabaseContextFactory _factory;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public DreamcoinService(DatabaseContextFactory factory)
    {
        _factory = factory;
        Log.Info("DreamcoinService initialized.");
    }

    /// <summary>
    ///   Gets the Dreamcoin balance for a player by CD key.
    /// </summary>
    /// <param name="cdKey">The player's CD key.</param>
    /// <returns>The Dreamcoin balance, or 0 if not found.</returns>
    public async Task<int> GetDreamcoins(string cdKey)
    {
        await using AmiaDbContext context = _factory.CreateDbContext();
        try
        {
            DreamcoinRecord? record = await context.DreamcoinRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.CdKey == cdKey);

            int balance = record?.Amount ?? 0;
            Log.Info($"GetDreamcoins for {cdKey}: balance={balance}");

            await NwTask.SwitchToMainThread();
            return balance;
        }
        catch (Exception e)
        {
            Log.Error($"Error getting Dreamcoins for {cdKey}: {e.Message}");
            await NwTask.SwitchToMainThread();
            return 0;
        }
    }

    /// <summary>
    ///   Sets the Dreamcoin balance for a player by CD key.
    /// </summary>
    /// <param name="cdKey">The player's CD key.</param>
    /// <param name="amount">The new Dreamcoin balance.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public async Task<bool> SetDreamcoins(string cdKey, int amount)
    {
        await using AmiaDbContext context = _factory.CreateDbContext();
        try
        {
            DreamcoinRecord? record = await context.DreamcoinRecords
                .FirstOrDefaultAsync(r => r.CdKey == cdKey);

            if (record == null)
            {
                Log.Warn($"No Dreamcoin record found for {cdKey}.");
                await NwTask.SwitchToMainThread();
                return false;
            }

            record.Amount = amount;
            await context.SaveChangesAsync();
            await NwTask.SwitchToMainThread();
            return true;
        }
        catch (Exception e)
        {
            Log.Error($"Error setting Dreamcoins for {cdKey}: {e.Message}");
            await NwTask.SwitchToMainThread();
            return false;
        }
    }

    /// <summary>
    ///   Adds Dreamcoins to a player's balance.
    /// </summary>
    /// <param name="cdKey">The player's CD key.</param>
    /// <param name="amount">The amount to add.</param>
    /// <returns>The new balance, or -1 on failure.</returns>
    public async Task<int> AddDreamcoins(string cdKey, int amount)
    {
        await using AmiaDbContext context = _factory.CreateDbContext();
        try
        {
            DreamcoinRecord? record = await context.DreamcoinRecords
                .FirstOrDefaultAsync(r => r.CdKey == cdKey);

            if (record == null)
            {
                Log.Warn($"No Dreamcoin record found for {cdKey}.");
                await NwTask.SwitchToMainThread();
                return -1;
            }

            record.Amount = (record.Amount ?? 0) + amount;
            await context.SaveChangesAsync();
            await NwTask.SwitchToMainThread();
            return record.Amount.Value;
        }
        catch (Exception e)
        {
            Log.Error($"Error adding Dreamcoins for {cdKey}: {e.Message}");
            await NwTask.SwitchToMainThread();
            return -1;
        }
    }

    /// <summary>
    ///   Removes Dreamcoins from a player's balance.
    /// </summary>
    /// <param name="cdKey">The player's CD key.</param>
    /// <param name="amount">The amount to remove.</param>
    /// <returns>The new balance, or -1 on failure.</returns>
    public async Task<int> RemoveDreamcoins(string cdKey, int amount)
    {
        await using AmiaDbContext context = _factory.CreateDbContext();
        try
        {
            DreamcoinRecord? record = await context.DreamcoinRecords
                .FirstOrDefaultAsync(r => r.CdKey == cdKey);

            if (record == null)
            {
                Log.Warn($"No Dreamcoin record found for {cdKey}.");
                await NwTask.SwitchToMainThread();
                return -1;
            }

            int currentAmount = record.Amount ?? 0;
            int newAmount = currentAmount - amount;

            if (newAmount < 0)
            {
                Log.Warn($"Cannot remove {amount} Dreamcoins from {cdKey}, only has {currentAmount}.");
                await NwTask.SwitchToMainThread();
                return -1;
            }

            record.Amount = newAmount;
            await context.SaveChangesAsync();
            await NwTask.SwitchToMainThread();
            return newAmount;
        }
        catch (Exception e)
        {
            Log.Error($"Error removing Dreamcoins for {cdKey}: {e.Message}");
            await NwTask.SwitchToMainThread();
            return -1;
        }
    }
}
