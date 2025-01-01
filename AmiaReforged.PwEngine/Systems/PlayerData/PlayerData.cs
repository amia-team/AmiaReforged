using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.PlayerData;

[ServiceBinding(typeof(PlayerData))]
public class PlayerData
{
    public Dictionary<string, AmiaPlayer> Players { get; } = new();

    public PlayerData()
    {
        NwModule.Instance.OnClientEnter += RegisterPlayer;
    }

    private void RegisterPlayer(ModuleEvents.OnClientEnter obj)
    {
        if(obj.Player.IsDM) return;
        
        Players.TryAdd(obj.Player.PlayerName, new AmiaPlayer());
    }
}