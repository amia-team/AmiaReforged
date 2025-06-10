using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Core.Services.PlaySessionLogging;

[ServiceBinding(typeof(PlaySessionHandler))]
public class PlaySessionHandler
{
    private readonly Dictionary<string, PlaySession?> _sessions = new();
    private readonly SchedulerService _scheduler;

    public PlaySessionHandler(SchedulerService schedulerService)
    {
        //Quick fix to get the scheduler service to inject properly in the playsession...
        _scheduler = schedulerService;
    }

    public void StartSessionFor(NwPlayer player)
    {
        _sessions.TryAdd(player.PlayerName, new(player, _scheduler));
    }

    public PlaySession? GetSessionFor(string name) => _sessions[name];

    public void EndSessionFor(NwPlayer player)
    {
        bool sessionExists = _sessions.TryGetValue(player.PlayerName, out PlaySession? session);
        if (sessionExists)
        {
            session?.EndSession();
        }
        
        if(!sessionExists) return;
        
        _sessions[player.PlayerName]!.Dispose();

        _sessions.Remove(player.PlayerName);
    }
}