using Anvil.API;
using NLog;

namespace AmiaReforged.Core.UserInterface;

public static class PcKeyUtils
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public static Guid GetPcKey(NwPlayer player)
    {
        Log.Info($"{player.PlayerName}");
        NwItem? pcKey = player.LoginCreature?.FindItemWithTag("ds_pckey");
        
        if (pcKey == null)
        {
            return Guid.Empty;
        }

        string pckeyGuid = pcKey.Name.Split('_')[1];
        return Guid.Parse(pckeyGuid);
    }
}