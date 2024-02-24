using AmiaReforged.Core.UserInterface;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.System.UI.PlayerTools;


[ServiceBinding(typeof(PlayerToolButtonInitializer))]
public class PlayerToolButtonInitializer : IInitializable
{
    [Inject] private WindowManager WindowManager { get; set; }
    
    public void Init()
    {
        NwModule.Instance.OnClientEnter += eventData => TryOpenWindow(eventData.Player);
        
        foreach (NwPlayer player in NwModule.Instance.Players)
        {
            if (player is { IsValid: true, IsConnected: true })
            {
                TryOpenWindow(player);
            }
        }
    }

    private void TryOpenWindow(NwPlayer eventDataPlayer)
    {
        if(eventDataPlayer.IsDM) return;
        
        WindowManager.OpenWindow<PlayerToolButtonView>(eventDataPlayer);
    }
}