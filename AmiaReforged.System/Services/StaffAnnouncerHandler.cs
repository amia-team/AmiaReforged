using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.System.Services;

[ServiceBinding(typeof(StaffAnnouncerHandler))]
public class StaffAnnouncerHandler
{
    [ScriptHandler("webhook_staff")]
    public void StaffDiscordAnnouncer(CallInfo info)
    {
        string message = NWScript.GetLocalString(NWScript.GetModule(), "staffMessage");
        if (string.Equals(message, ""))
        {
            message = "Empty Variable";
        }

        var instance = new JoinWebhookService();
        _ = instance.LaunchStaffMessage(message);
    }
}