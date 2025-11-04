using System.Threading.Tasks;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls;

public interface IPlayerStallEventBroadcaster
{
    Task BroadcastSellerRefreshAsync(long stallId);
}
