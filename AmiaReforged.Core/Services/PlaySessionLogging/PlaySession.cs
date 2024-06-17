using AmiaReforged.Core;
using AmiaReforged.Core.Models;
using AmiaReforged.Core.Services;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NLog.Fluent;

#pragma warning disable CS8602 // Dereference of a possibly null reference.

namespace AmiaReforged.System.Models;

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


    public PlaySession(NwPlayer player, SchedulerService scheduler)
    {
        Player = player;
        SessionStart = DateTime.UtcNow;
        DbFactory = new DatabaseContextFactory();
        Scheduler = scheduler;

        StartSession();
        NwModule.Instance.OnHeartbeat += SavePlayTime;
    }

    private void SavePlayTime(ModuleEvents.OnHeartbeat obj)
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

    private void StartSession()
    {
        AmiaDbContext context = DbFactory.CreateDbContext();

        // TODO: Change this to use a strategy pattern when we have more than one login type.
        if (!Player.IsDM) return;

        context.DmLogins.Add(new DmLogin
        {
            CdKey = Player.CDKey,
            LoginName = Player.PlayerName,
            SessionStart = SessionStart
        });

        context.SaveChanges();

        LoginId = context.DmLogins.Where(x => x.CdKey == Player.CDKey).OrderByDescending(x => x.SessionStart).First()
            .LoginNumber;
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
        ((IDisposable)Scheduler).Dispose();
    }
}