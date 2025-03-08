using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.System.Services;

[ServiceBinding(typeof(StaffAnnouncerHandler))]
public class StaffAnnouncerHandler
{
    [ScriptHandler(scriptName: "webhook_staff")]
    public void StaffDiscordAnnouncer(CallInfo info)
    {
        string message = NWScript.GetLocalString(NWScript.GetModule(), sVarName: "staffMessage");
        if (string.Equals(message, b: "")) message = "Empty Variable";

        var instance = new JoinWebhookService();
        _ = instance.LaunchStaffMessage(message);
    }
}