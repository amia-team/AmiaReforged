namespace AmiaReforged.PwEngine.Systems.WorldEngine.Events;

public interface IMediatorRegistry
{
    Dictionary<WorldEventType, IEventMediator> Registered { get; set; }
    bool Register(IEventMediator mediator);
}
