using AmiaReforged.System.Dynamic.Quest;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core;

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
        NwCreature? questGiver = info.ObjectSelf as NwCreature;
        NwCreature? lastSpeaker = NWScript.GetLastSpeaker().ToNwObject<NwCreature>();
        
        if(!lastSpeaker.IsPlayerControlled(out NwPlayer? player)) return;
        MiniQuest quest = new(questGiver, player);
        quest.ProcessReward();
    }
}