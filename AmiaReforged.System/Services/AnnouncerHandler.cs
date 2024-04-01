using AmiaReforged.System.Webhooks;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;
using JoinWebhookService;
using NWN.Core; 

namespace AmiaReforged.System.Services;
[ServiceBinding(typeof(AnnouncerHandler))]

public class AnnouncerHandler
{

[ScriptHandler("webhook_announce")]
public void DiscordAnnouncer(CallInfo info)
{
    string message = NWScript.GetLocalString(NWScript.GetModule(),"announcerMessage");
    if(string.equals(message,""))
    {
        message = "Empty Variable";
    }
    var instance = new JoinWebhookService();
    global::System.Object discordMessage = instance.LaunchDiscordMessage(message);
}
}
