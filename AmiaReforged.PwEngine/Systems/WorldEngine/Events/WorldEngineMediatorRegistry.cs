using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Events;

[ServiceBinding(typeof(WorldEngineMediatorRegistry))]
public class WorldEngineMediatorRegistry : IMediatorRegistry
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public Dictionary<WorldEventType, IEventMediator> Registered { get; set; } = new();

    public bool Register(IEventMediator mediator)
    {
        return Registered.TryAdd(mediator.EventType, mediator);
    }
}
