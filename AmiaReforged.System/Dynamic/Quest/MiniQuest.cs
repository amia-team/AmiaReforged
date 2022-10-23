using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.Dynamic.Quest;

public class MiniQuest
{
    private readonly NwCreature? _questGiver;
    private readonly NwPlayer? _player;

    public MiniQuest(NwCreature? questGiver, NwPlayer? player)
    {
        _questGiver = questGiver;
        _player = player;
    }

    public void ProcessReward()
    {
        if (_questGiver == null || _player == null) return;
        if (_player.LoginCreature == null) return;

        string questTurnInItem = NWScript.GetLocalString(_questGiver, DynamicQuestLocals.MiniQuest.QuestItem);

        List<NwItem> questItems = _player.LoginCreature.Inventory.Items.Where(x => x.Tag == questTurnInItem).ToList();

        bool hasNoQuestItems = questItems.Count == 0;
        string speakString = hasNoQuestItems
            ? NWScript.GetLocalString(_questGiver, DynamicQuestLocals.MiniQuest.QuestFailedLine)
            : NWScript.GetLocalString(_questGiver, DynamicQuestLocals.MiniQuest.QuestDoneLine);

        DoRewardForEachItem(questItems);

        _questGiver.SpeakString(speakString);
    }

    private void DoRewardForEachItem(List<NwItem> questItems)
    {
        foreach (NwItem item in questItems)
        {
            DoExperienceReward();
            DoGoldReward();
            DoItemReward();
            item.Destroy();
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