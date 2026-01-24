using AmiaReforged.Core.Models;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.Core.Services;

/// <summary>
///   Service for managing player playtime records in the database.
///   Tracks weekly playtime and minutes accumulated toward DC awards.
/// </summary>
[ServiceBinding(typeof(PlayerPlaytimeService))]
public class PlayerPlaytimeService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly DatabaseContextFactory _factory;

    public PlayerPlaytimeService(DatabaseContextFactory factory)
    {
        _factory = factory;
        Log.Info("PlayerPlaytimeService initialized.");
    }

    /// <summary>
    ///   Gets the current week's start date (Monday 00:00 UTC).
    /// </summary>
    public static DateTime GetCurrentWeekStart()
    {
        DateTime now = DateTime.UtcNow;
        int daysToSubtract = ((int)now.DayOfWeek + 6) % 7; // Monday = 0
        return now.Date.AddDays(-daysToSubtract);
    }

    /// <summary>
    ///   Gets or creates a playtime record for the current week.
    /// </summary>
    /// <param name="cdKey">The player's CD key.</param>
    /// <returns>The playtime record for the current week.</returns>
    public async Task<PlayerPlaytimeRecord> GetOrCreateCurrentWeekRecord(string cdKey)
    {
        await using AmiaDbContext context = _factory.CreateDbContext();
        DateTime weekStart = GetCurrentWeekStart();

        PlayerPlaytimeRecord? record = await context.PlayerPlaytimeRecords
            .FirstOrDefaultAsync(r => r.CdKey == cdKey && r.WeekStart == weekStart);

        if (record == null)
        {
            record = new PlayerPlaytimeRecord
            {
                CdKey = cdKey,
                WeekStart = weekStart,
                MinutesPlayed = 0,
                MinutesTowardNextDc = 0,
                LastUpdated = DateTime.UtcNow
            };

            context.PlayerPlaytimeRecords.Add(record);
            await context.SaveChangesAsync();
            Log.Info($"Created new weekly playtime record for {cdKey}, week starting {weekStart:yyyy-MM-dd}");
        }

        return record;
    }

    /// <summary>
    ///   Adds playtime minutes to a player's current week record.
    /// </summary>
    /// <param name="cdKey">The player's CD key.</param>
    /// <param name="minutes">Minutes to add.</param>
    /// <returns>The updated record with current accumulated minutes toward next DC.</returns>
    public async Task<PlayerPlaytimeRecord> AddPlaytimeMinutes(string cdKey, int minutes)
    {
        await using AmiaDbContext context = _factory.CreateDbContext();
        DateTime weekStart = GetCurrentWeekStart();

        PlayerPlaytimeRecord? record = await context.PlayerPlaytimeRecords
            .FirstOrDefaultAsync(r => r.CdKey == cdKey && r.WeekStart == weekStart);

        if (record == null)
        {
            record = new PlayerPlaytimeRecord
            {
                CdKey = cdKey,
                WeekStart = weekStart,
                MinutesPlayed = 0,
                MinutesTowardNextDc = 0,
                LastUpdated = DateTime.UtcNow
            };
            context.PlayerPlaytimeRecords.Add(record);
        }

        record.MinutesPlayed += minutes;
        record.MinutesTowardNextDc += minutes;
        record.LastUpdated = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return record;
    }

    /// <summary>
    ///   Resets the minutes toward next DC after an award is given.
    /// </summary>
    /// <param name="cdKey">The player's CD key.</param>
    /// <param name="minutesToSubtract">Minutes to subtract (typically MinutesPerDc).</param>
    public async Task ResetMinutesTowardNextDc(string cdKey, int minutesToSubtract)
    {
        await using AmiaDbContext context = _factory.CreateDbContext();
        DateTime weekStart = GetCurrentWeekStart();

        PlayerPlaytimeRecord? record = await context.PlayerPlaytimeRecords
            .FirstOrDefaultAsync(r => r.CdKey == cdKey && r.WeekStart == weekStart);

        if (record != null)
        {
            record.MinutesTowardNextDc = Math.Max(0, record.MinutesTowardNextDc - minutesToSubtract);
            record.LastUpdated = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    ///   Gets the minutes accumulated toward the next DC award.
    /// </summary>
    /// <param name="cdKey">The player's CD key.</param>
    /// <returns>Minutes accumulated, or 0 if no record exists.</returns>
    public async Task<int> GetMinutesTowardNextDc(string cdKey)
    {
        await using AmiaDbContext context = _factory.CreateDbContext();
        DateTime weekStart = GetCurrentWeekStart();

        PlayerPlaytimeRecord? record = await context.PlayerPlaytimeRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.CdKey == cdKey && r.WeekStart == weekStart);

        return record?.MinutesTowardNextDc ?? 0;
    }

    /// <summary>
    ///   Gets the total minutes played for the current week.
    /// </summary>
    /// <param name="cdKey">The player's CD key.</param>
    /// <returns>Total minutes played this week, or 0 if no record exists.</returns>
    public async Task<int> GetWeeklyMinutesPlayed(string cdKey)
    {
        await using AmiaDbContext context = _factory.CreateDbContext();
        DateTime weekStart = GetCurrentWeekStart();

        PlayerPlaytimeRecord? record = await context.PlayerPlaytimeRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.CdKey == cdKey && r.WeekStart == weekStart);

        return record?.MinutesPlayed ?? 0;
    }

    /// <summary>
    ///   Gets the total all-time playtime for a player.
    /// </summary>
    /// <param name="cdKey">The player's CD key.</param>
    /// <returns>Total minutes played across all weeks.</returns>
    public async Task<int> GetTotalMinutesPlayed(string cdKey)
    {
        await using AmiaDbContext context = _factory.CreateDbContext();

        int totalMinutes = await context.PlayerPlaytimeRecords
            .AsNoTracking()
            .Where(r => r.CdKey == cdKey)
            .SumAsync(r => r.MinutesPlayed);

        return totalMinutes;
    }

    /// <summary>
    ///   Gets playtime records for a player, optionally filtered by date range.
    /// </summary>
    /// <param name="cdKey">The player's CD key.</param>
    /// <param name="fromDate">Optional start date filter.</param>
    /// <param name="toDate">Optional end date filter.</param>
    /// <returns>List of playtime records.</returns>
    public async Task<List<PlayerPlaytimeRecord>> GetPlaytimeHistory(string cdKey, DateTime? fromDate = null, DateTime? toDate = null)
    {
        await using AmiaDbContext context = _factory.CreateDbContext();

        IQueryable<PlayerPlaytimeRecord> query = context.PlayerPlaytimeRecords
            .AsNoTracking()
            .Where(r => r.CdKey == cdKey);

        if (fromDate.HasValue)
            query = query.Where(r => r.WeekStart >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(r => r.WeekStart <= toDate.Value);

        return await query.OrderByDescending(r => r.WeekStart).ToListAsync();
    }
}
