using AmiaReforged.Core.Models;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core.NWNX;

namespace AmiaReforged.Core.Services.PlaySessionLogging;

[ServiceBinding(typeof(DmLoginSessionService))]
public class DmLoginSessionService
{
    private readonly PlaySessionHandler _playSession;
    private readonly DatabaseContextFactory _dbFactory;

    public DmLoginSessionService(PlaySessionHandler playSession, DatabaseContextFactory dbFactory)
    {
        _playSession = playSession;
        _dbFactory = dbFactory;

        NwModule.Instance.OnClientEnter += StartDmSession;
        NwModule.Instance.OnClientLeave += EndDmSession;
    }

    private void StartDmSession(ModuleEvents.OnClientEnter obj)
    {
        if (!obj.Player.IsDM) return;

        string environment = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE");

        if (environment != "live") return;

        if (DidBootUnauthorizedUser(obj.Player)) return;

        _playSession.StartSessionFor(obj.Player);
    }

    private bool DidBootUnauthorizedUser(NwPlayer Player)
    {

        AmiaDbContext context = _dbFactory.CreateDbContext();

        List<Dm> allDms = context.Dms.ToList();

        if (allDms.Any(dm => dm.CdKey == Player.CDKey)) return false;

        Player.BootPlayer("Unauthorized DM Access");

        return true;
    }

    private void EndDmSession(ModuleEvents.OnClientLeave obj)
    {
        if (!obj.Player.IsDM) return;

        _playSession.EndSessionFor(obj.Player);
    }
}
