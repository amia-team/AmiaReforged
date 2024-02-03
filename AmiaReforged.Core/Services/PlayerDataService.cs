using AmiaReforged.Core.Helpers;
using AmiaReforged.Core.Models;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.Core.Services;

/// <summary>
///   Service for handling player data from the database.
/// </summary>
[ServiceBinding(typeof(PlayerDataService))]
public class PlayerDataService
{
    private readonly DatabaseContextFactory _factory;
    private readonly NwTaskHelper _taskHelper;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();


    /// <summary>
    ///  Constructor for PlayerDataService.
    /// </summary>
    /// <param name="dbContext"> The database context. </param>
    /// <param name="taskHelper"> Awaitable TaskHelper that can be mocked for testing. </param>
    public PlayerDataService(DatabaseContextFactory dbContext, NwTaskHelper taskHelper)
    {
        _factory = dbContext;
        _taskHelper = taskHelper;
        Log.Info("PlayerDataService initialized.");
    }

    public async Task<IEnumerable<PlayerCharacter>> GetPlayerCharacters(string cdkey)
    {
        IEnumerable<PlayerCharacter> characters = new List<PlayerCharacter>();
        AmiaDbContext amiaDbContext = _factory.CreateDbContext();
        try
        {
            Player? player = await amiaDbContext.Players
                .Include(p => p.PlayerCharacters) // Eager load PlayerCharacters
                .FirstOrDefaultAsync(p => p.CdKey == cdkey);
            characters = player?.PlayerCharacters ?? new List<PlayerCharacter>();
        }
        catch (Exception e)
        {
            Log.Error($"Error getting player characters from database for player {cdkey}: {e.Message}");
        }

        await _taskHelper.TrySwitchToMainThread();

        return characters;
    }
}