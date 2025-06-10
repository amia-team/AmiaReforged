using AmiaReforged.Core.Models;
using Anvil.API;
using Anvil.Services;
using NLog;

#pragma warning disable CS8602 // Dereference of a possibly null reference.

namespace AmiaReforged.Core.Services.PlaySessionLogging;

public class PlaySession : IDisposable
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private const double OneMinute = 1;
    private NwPlayer Player { get; }

    private int LoginId { get; set; }
    private DateTime SessionStart { get; init; }
    private DateTime SessionEnd { get; set; }
    private SchedulerService Scheduler { get; set; }
    private DatabaseContextFactory DbFactory { get; set; }
    
    private bool Active => SessionEnd == default;
    public ScheduledTask SavePlayTimeCallback { get; set; }

    public PlaySession(NwPlayer player, SchedulerService scheduler)
    {
        Player = player;
        SessionStart = DateTime.UtcNow;
        DbFactory = new();
        Scheduler = scheduler;

        StartSession();
        SavePlayTimeCallback = Scheduler.ScheduleRepeating(SavePlayTime, TimeSpan.FromMinutes(OneMinute));
    }

    private void StartSession()
    {
        AmiaDbContext context = DbFactory.CreateDbContext();

        // TODO: Change this to use a strategy pattern when we have more than one login type.
        if (!Player.IsDM) return;

        context.DmLogins.Add(new()
        {
            CdKey = Player.CDKey,
            LoginName = Player.PlayerName,
            SessionStart = SessionStart
        });

        context.SaveChanges();

        LoginId = context.DmLogins.Where(x => x.CdKey == Player.CDKey).OrderByDescending(x => x.SessionStart).First()
            .LoginNumber;
    }

    private void SavePlayTime()
    {
        if (!Player.IsDM) return;
        if(!Active) return;
        
        Log.Info($"Saving playtime for {Player.PlayerName} with login id {LoginId}");

        AmiaDbContext context = DbFactory.CreateDbContext();

        int playTimeInMinutes = PlayTimeInMinutes();

        DmLogin? login = context.DmLogins.Find(LoginId);

        login.PlayTime = playTimeInMinutes;

        context.SaveChanges();
    }

    private int PlayTimeInMinutes() => (int)Math.Ceiling((DateTime.UtcNow - SessionStart).TotalMinutes);

    public void EndSession()
    {
        SessionEnd = DateTime.UtcNow;

        AmiaDbContext context = DbFactory.CreateDbContext();

        DmLogin? login = context.DmLogins.Find(LoginId);

        login.SessionEnd = SessionEnd;
        login.PlayTime = PlayTimeInMinutes();

        context.SaveChanges();
    }

    public void Dispose()
    {
        SavePlayTimeCallback.Dispose();
    }
}