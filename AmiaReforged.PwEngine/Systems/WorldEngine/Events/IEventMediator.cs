namespace AmiaReforged.PwEngine.Systems.WorldEngine.Events;

public interface IEventMediator
{
    WorldEventType EventType { get; init; }
}

public enum WorldEventType
{
    BuyItem,
    SellItem,
}
