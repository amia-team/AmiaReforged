using AmiaReforged.Core.UserInterface;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerId;

[ServiceBinding(typeof(PlayerIdService))]
public class PlayerIdService
{
    private readonly Dictionary<NwPlayer, Guid> _playerKeys = new();

    public PlayerIdService(EventService service)
    {
        NwModule.Instance.OnAcquireItem += ReCache;
        NwModule.Instance.OnClientEnter += CachePcKey;
        NwModule.Instance.OnClientLeave += ClearPlayerFromCache;
    }

    private void ClearPlayerFromCache(ModuleEvents.OnClientLeave obj)
    {
        if (obj.Player.IsDM) return;
        _playerKeys.Remove(obj.Player);
    }

    private void ReCache(ModuleEvents.OnAcquireItem obj)
    {
        NwItem? objItem = obj.Item;
        if (objItem == null) return;
        if (objItem.Tag != "ds_pckey") return;
        if (!obj.AcquiredBy.IsPlayerControlled(out NwPlayer? player)) return;

        Guid key = PcKeyUtils.GetPcKey(player);

        _playerKeys[player] = key;
    }

    private void CachePcKey(ModuleEvents.OnClientEnter obj)
    {
        if (obj.Player.IsDM) return;

        Guid key = PcKeyUtils.GetPcKey(obj.Player);

        _playerKeys.TryAdd(obj.Player, key);
    }

    public Guid GetPlayerKey(NwPlayer player) => _playerKeys[player];
}