using Anvil.API;
using NLog;

namespace AmiaReforged.Core.Services.Sailing;

public static class SailingWindowLifecycle
{
    // Callers remove window state and subscriptions first, so a stale native
    // token or a reentrant Close event cannot leave an owned window behind.
    public static void Close(NuiWindowToken token)
    {
        try { if (token.Player.IsValid) token.Close(); }
        catch (Exception ex) { LogManager.GetCurrentClassLogger().Debug(ex, "Sailing window was already unavailable during close."); }
    }
}
