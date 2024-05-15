using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.System.Services;

[ServiceBinding(typeof(DmLoginService))]
public class DmLoginService
{
    private readonly SessionHandler _session;

    public DmLoginService(SessionHandler session)
    {
        _session = session;
        
        // Register for OnClientEnter event for DMs
        // e.g.: NwModule.Instance.OnClientEnter += StartDmSession;
        // Register for OnClientDisconnect event for DMs
        // e.g.: NwModule.Instance.OnClientEnter += EndDmSession;
    }
}

[ServiceBinding(typeof(SessionHandler))]
public class SessionHandler
{
    
}