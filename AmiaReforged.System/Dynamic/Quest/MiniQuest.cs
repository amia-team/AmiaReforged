using Anvil.API;
using NLog;
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
            DoExperienceReward(_player!);
            DoGoldReward(_player!);
            item.Destroy();
        }
    }

    private void DoExperienceReward(NwPlayer nwPlayer)
    {
        int xpReward = NWScript.GetLocalInt(_questGiver, DynamicQuestLocals.MiniQuest.XpReward);
        nwPlayer.GiveXp(xpReward);
    }

    private void DoGoldReward(NwPlayer nwPlayer)
    {
        int goldReward = NWScript.GetLocalInt(_questGiver, DynamicQuestLocals.MiniQuest.GoldReward);
        nwPlayer.LoginCreature!.GiveGold(goldReward);
    }
}