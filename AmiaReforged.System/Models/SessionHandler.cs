using Anvil.Services;

namespace AmiaReforged.System.Services;

[ServiceBinding(typeof(SessionHandler))]
public class SessionHandler
{
    private Dictionary<string, Session> _sessions = new();

    public void StartSessionFor(string name)
    {
        _sessions.TryAdd(name, new Session());
    }

    public Session GetSessionFor(string name) => _sessions[name];

    public void EndSessionFor(string name)
    {
        _sessions.Remove(name);
    }
}