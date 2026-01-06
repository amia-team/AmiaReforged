using AmiaReforged.Core.Models;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.Core.Services;

/// <summary>
/// Service for managing banned CD keys.
/// </summary>
[ServiceBinding(typeof(BanService))]
public class BanService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly DatabaseContextFactory _factory;

    public BanService(DatabaseContextFactory factory)
    {
        _factory = factory;
        Log.Info("BanService initialized.");
    }

    /// <summary>
    /// Gets all banned CD keys.
    /// </summary>
    public async Task<List<Ban>> GetAllBansAsync()
    {
        await using AmiaDbContext context = _factory.CreateDbContext();
        try
        {
            return await context.Bans.OrderBy(b => b.CdKey).ToListAsync();
        }
        catch (Exception e)
        {
            Log.Error($"Error getting all bans: {e.Message}");
            return [];
        }
    }

    /// <summary>
    /// Checks if a CD key is banned.
    /// </summary>
    public async Task<bool> IsBannedAsync(string cdKey)
    {
        await using AmiaDbContext context = _factory.CreateDbContext();
        try
        {
            return await context.Bans.AnyAsync(b => b.CdKey == cdKey);
        }
        catch (Exception e)
        {
            Log.Error($"Error checking ban status for {cdKey}: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Bans a CD key.
    /// </summary>
    /// <returns>True if successful, false if already banned or error.</returns>
    public async Task<bool> BanAsync(string cdKey)
    {
        await using AmiaDbContext context = _factory.CreateDbContext();
        try
        {
            // Check if already banned
            if (await context.Bans.AnyAsync(b => b.CdKey == cdKey))
            {
                Log.Warn($"CD Key {cdKey} is already banned.");
                return false;
            }

            Ban ban = new() { CdKey = cdKey };
            context.Bans.Add(ban);
            await context.SaveChangesAsync();

            Log.Info($"CD Key {cdKey} has been banned.");
            return true;
        }
        catch (Exception e)
        {
            Log.Error($"Error banning {cdKey}: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Unbans a CD key.
    /// </summary>
    /// <returns>True if successful, false if not banned or error.</returns>
    public async Task<bool> UnbanAsync(string cdKey)
    {
        await using AmiaDbContext context = _factory.CreateDbContext();
        try
        {
            Ban? ban = await context.Bans.FirstOrDefaultAsync(b => b.CdKey == cdKey);
            if (ban == null)
            {
                Log.Warn($"CD Key {cdKey} is not banned.");
                return false;
            }

            context.Bans.Remove(ban);
            await context.SaveChangesAsync();

            Log.Info($"CD Key {cdKey} has been unbanned.");
            return true;
        }
        catch (Exception e)
        {
            Log.Error($"Error unbanning {cdKey}: {e.Message}");
            return false;
        }
    }
}
