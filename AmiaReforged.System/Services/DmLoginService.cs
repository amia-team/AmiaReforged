using Anvil.Services;

namespace AmiaReforged.System.Services;

[ServiceBinding(typeof(DmLoginService))]
public class DmLoginService
{
    private readonly SessionHandler _session;

    public DmLoginService(SessionHandler session)
    {
        _session = session;
        
        // Register for OnLogin event for DMs
        
        // R
    }
    
}

[ServiceBinding(typeof(SessionHandler))]
public class SessionHandler
{
    
}