using System.Dynamic;
using Anvil.Services;
using NLog;

namespace AmiaReforged.System.Services;

[ServiceBinding(typeof(DynamicQuestHandler))]
public class DynamicQuestHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public DynamicQuestHandler()
    {
        Log.Info("DynamicQuestHandler initialized.");
    }

    
    
    [ScriptHandler("jes_miniquest")]
    public void OnMiniQuest(CallInfo info)
    {
        Log.Info("Miniquest started.");
    }
}