namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls;

public interface IPlayerStallEventBroadcaster
{
    Task BroadcastSellerRefreshAsync(long stallId);
}
