using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.System.Services;
[ServiceBinding(typeof(AnnouncerHandler))]

public class AnnouncerHandler
{

[ScriptHandler("webhook_announce")]
public void DiscordAnnouncer(CallInfo info)
{
    string message = NWScript.GetLocalString(NWScript.GetModule(),"announcerMessage");
    if (string.Equals(message, ""))
    {
        message = "Empty Variable";
    }
    var instance = new JoinWebhookService();
    _ = instance.LaunchDiscordMessage(message);
}
}
