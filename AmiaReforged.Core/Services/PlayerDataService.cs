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
    private readonly AmiaDbContext _ctx;
    private readonly NwTaskHelper _taskHelper;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();


    /// <summary>
    ///  Constructor for PlayerDataService.
    /// </summary>
    /// <param name="dbContext"> The database context. </param>
    /// <param name="taskHelper"> Awaitable TaskHelper that can be mocked for testing. </param>
    public PlayerDataService(AmiaDbContext dbContext, NwTaskHelper taskHelper)
    {
        _ctx = dbContext;
        _taskHelper = taskHelper;
        Log.Info("PlayerDataService initialized.");
    }

    public async Task<IEnumerable<PlayerCharacter>> GetPlayerCharacters(string cdkey)
    {
        IEnumerable<PlayerCharacter> characters = new List<PlayerCharacter>();

        try
        {
            Player? player = await _ctx.Players
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
    //
    // public async Task<PlayerCharacter> GetPlayerCharacter(string cdkey, Guid characterId)
    // {
    //     PlayerCharacter character = new PlayerCharacter();
    //
    //     try
    //     {
    //         Player? player = await _ctx.Players
    //             .Include(p => p.PlayerCharacters)
    //             .ThenInclude(pc => pc.Items)// Eager load PlayerCharacters
    //             .FirstOrDefaultAsync(p => p.CdKey == cdkey);
    //         character = player?.PlayerCharacters.FirstOrDefault(c => c.Id == characterId) ?? new PlayerCharacter();
    //     }
    //     catch (Exception e)
    //     {
    //         Log.Error($"Error getting player character from database for player {cdkey}: {e.Message}");
    //     }
    //
    //     await _taskHelper.TrySwitchToMainThread();
    //
    //     return character;
    // }
}