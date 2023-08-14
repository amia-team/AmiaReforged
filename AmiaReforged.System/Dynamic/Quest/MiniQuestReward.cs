using Anvil.API;
using Npgsql.TypeMapping;
using NWN.Core;

namespace AmiaReforged.System.Dynamic.Quest;

public class MiniQuestReward : IMiniQuestRewardStrategy
{
    private NwCreature? _questGiver;
    private NwPlayer? _player;

    public void DoRewardForEachItem(List<NwItem> questItems, NwCreature? questGiver, NwPlayer? player)
    {
        _player = player;
        _questGiver = questGiver;
        int maxItemTaken = NWScript.GetLocalInt(questGiver, DynamicQuestLocals.MiniQuest.TakeMax);
        for (int i = 0; i < questItems.Count; i++)
        {
            if (i > maxItemTaken && maxItemTaken > 0) break;
            DoExperienceReward();
            DoGoldReward();
            DoItemReward();
            questItems[i].Destroy();
        }
    }
    private void DoExperienceReward()
    {
        int xpReward = NWScript.GetLocalInt(_questGiver, DynamicQuestLocals.MiniQuest.XpReward);
        _player!.GiveXp(xpReward);
    }

    private void DoGoldReward()
    {
        int goldReward = NWScript.GetLocalInt(_questGiver, DynamicQuestLocals.MiniQuest.GoldReward);
        _player!.LoginCreature!.GiveGold(goldReward);
    }

    private void DoItemReward()
    {
        string itemReward = NWScript.GetLocalString(_questGiver, DynamicQuestLocals.MiniQuest.ItemReward);
        if (itemReward != string.Empty)
        { 
            NwItem.Create(itemReward, _player!.LoginCreature); 
        }
    }
}