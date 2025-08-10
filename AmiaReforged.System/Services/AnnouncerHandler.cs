using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.System.Services;

[ServiceBinding(typeof(AnnouncerHandler))]
public class AnnouncerHandler
{
    [ScriptHandler(scriptName: "webhook_announce")]
    public void DiscordAnnouncer(CallInfo info)
    {
        string message = NWScript.GetLocalString(NWScript.GetModule(), sVarName: "announcerMessage");
        if (string.Equals(message, b: "")) message = "Empty Variable";

        JoinWebhookService instance = new JoinWebhookService();
        _ = instance.LaunchDiscordMessage(message);
    }
}