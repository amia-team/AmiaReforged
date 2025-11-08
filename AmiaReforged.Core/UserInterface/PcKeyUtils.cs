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

        if (!Guid.TryParse(pckeyGuid, out Guid pcKeyParsed))
        {
            player.SendServerMessage("Your PC key is invalid. Please contact a DM.", ColorConstants.Orange);
            return Guid.Empty;
        }

        return pcKeyParsed;
    }
}
